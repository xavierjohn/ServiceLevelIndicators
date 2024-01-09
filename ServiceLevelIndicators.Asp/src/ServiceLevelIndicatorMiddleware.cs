namespace ServiceLevelIndicators;

using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Controllers;

internal sealed class ServiceLevelIndicatorMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ServiceLevelIndicator _serviceLevelIndicator;

    public ServiceLevelIndicatorMiddleware(RequestDelegate next, ServiceLevelIndicator serviceLevelIndicator)
    {
        _next = next;
        _serviceLevelIndicator = serviceLevelIndicator;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var metadata = context.GetEndpoint()?.Metadata;
        if (metadata == null || !ShouldEmitMetrics(metadata))
        {
            await _next(context);
            return;
        }

        var operation = GetOperation(context, metadata);
        var attributes = GetMeasuredAttributes(context, metadata);

        using var measuredOperation = _serviceLevelIndicator.StartLatencyMeasureOperation(operation, attributes);
        SetCustomerResourceIdFromAttribute(context, metadata, measuredOperation);
        AddSliFeatureToHttpContext(context, measuredOperation);
        await _next(context);
        UpdateOperationWithResponseStatus(context, measuredOperation);
        measuredOperation.AddAttribute("http.request.method", context.Request.Method);
        RemoveSliFeatureFromHttpContext(context);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void SetCustomerResourceIdFromAttribute(HttpContext context, EndpointMetadataCollection metadata, MeasuredOperationLatency measuredOperation)
    {
        var customerResourceId = GetCustomerResourceIdAttributes(context, metadata);
        if (customerResourceId is not null)
            measuredOperation.CustomerResourceId = customerResourceId;
    }

    private static void UpdateOperationWithResponseStatus(HttpContext context, MeasuredOperationLatency measuredOperation)
    {
        var statusCode = context.Response.StatusCode;
        measuredOperation.AddAttribute("http.response.status_code", statusCode);
        var activityCode = statusCode switch
        {
            >= StatusCodes.Status500InternalServerError => ActivityStatusCode.Error,
            >= StatusCodes.Status200OK and < StatusCodes.Status300MultipleChoices => ActivityStatusCode.Ok,
            _ => ActivityStatusCode.Unset,
        };
        measuredOperation.SetActivityStatusCode(activityCode);
    }


    private bool ShouldEmitMetrics(EndpointMetadataCollection metadata) =>
        _serviceLevelIndicator.ServiceLevelIndicatorOptions.AutomaticallyEmitted || GetSliAttribute(metadata) is not null;

    private static ServiceLevelIndicatorAttribute? GetSliAttribute(EndpointMetadataCollection metaData) =>
        metaData.GetMetadata<ServiceLevelIndicatorAttribute>();

    private static string GetOperation(HttpContext context, EndpointMetadataCollection metadata)
    {
        var sli = GetSliAttribute(metadata);
        string operation;

        if (sli is null || string.IsNullOrEmpty(sli.Operation))
        {
            var description = metadata.GetMetadata<ControllerActionDescriptor>();
            var path = description?.AttributeRouteInfo?.Template ?? context.Request.Path;
            operation = context.Request.Method + " " + path;
        }
        else
            operation = sli.Operation;

        return operation;
    }

    private static string? GetCustomerResourceIdAttributes(HttpContext context, EndpointMetadataCollection metadata)
    {
        var measures = metadata.OfType<CustomerResourceIdMetadata>().ToArray();
        var count = measures.Length;

        if (count == 0)
            return null;

        if (count > 1)
            throw new InvalidOperationException("Multiple " + nameof(CustomerResourceIdAttribute) + " defined.");

        var values = context.Request.RouteValues;
        var measure = measures[0];
        var value = values.TryGetValue(measure.RouteValueName, out var val) ? val : default;
        return value?.ToString();
    }

    private static KeyValuePair<string, object?>[] GetMeasuredAttributes(HttpContext context, EndpointMetadataCollection metadata)
    {
        var measures = metadata.OfType<MeasureMetadata>().ToArray();
        var count = measures.Length;

        if (count == 0)
            return [];

        var values = context.Request.RouteValues;
        var attributes = new KeyValuePair<string, object?>[count];

        for (var i = 0; i < count; i++)
        {
            var measure = measures[i];
            var value = values.TryGetValue(measure.RouteValueName, out var val) ? val : default;
            attributes[i] = KeyValuePair.Create(measure.AttributeName, value);
        }

        return attributes;
    }

    private void AddSliFeatureToHttpContext(HttpContext context, MeasuredOperationLatency measuredOperation)
    {
        if (context.Features.Get<IServiceLevelIndicatorFeature>() != null)
            throw new InvalidOperationException($"Another instance of {nameof(ServiceLevelIndicatorFeature)} already exists. Only one instance of {nameof(ServiceLevelIndicatorMiddleware)} can be configured for an application.");

        context.Features.Set<IServiceLevelIndicatorFeature>(new ServiceLevelIndicatorFeature(measuredOperation));
    }

    private static void RemoveSliFeatureFromHttpContext(HttpContext context) =>
        context.Features.Set<IServiceLevelIndicatorFeature?>(null);

}
