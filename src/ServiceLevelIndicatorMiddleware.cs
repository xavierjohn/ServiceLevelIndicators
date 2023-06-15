namespace Asp.ServiceLevelIndicators;

using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
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
        var metaData = context.Features.Get<IEndpointFeature>()?.Endpoint?.Metadata;
        ArgumentNullException.ThrowIfNull(metaData);
        if (!ShouldEmitMetrics(metaData))
        {
            await _next(context);
            return;
        }

        AddSliFeatureToHttpContext(context);
        var operation = GetOperation(context, metaData);
        using var measuredOperation = _serviceLevelIndicator.StartLatencyMeasureOperation(operation);

        await _next(context);
        UpdateOperationWithResponseStatus(context, measuredOperation);
        RemoveSliFeatureFromHttpContext(context);
    }

    private static void UpdateOperationWithResponseStatus(HttpContext context, LatencyMeasureOperation measuredOperation)
    {
        var statusCode = context.Response.StatusCode;
        measuredOperation.SetHttpStatusCode(statusCode);
        measuredOperation.SetState((statusCode >= 200 && statusCode < 300) ? System.Diagnostics.ActivityStatusCode.Ok : System.Diagnostics.ActivityStatusCode.Error);
        var customerResourceId = GetCustomerResourceId(context);
        measuredOperation.SetCustomerResourceId(customerResourceId);
    }

    private bool ShouldEmitMetrics(EndpointMetadataCollection metaData) =>
        _serviceLevelIndicator.ServiceLevelIndicatorOptions.AutomaticallyEmitted || GetSliAttribute(metaData) is not null;

    private static ServiceLevelIndicatorAttribute? GetSliAttribute(EndpointMetadataCollection metaData) =>
        metaData.GetMetadata<ServiceLevelIndicatorAttribute>();

    private static string GetCustomerResourceId(HttpContext context)
    {
        var feature = context.Features.Get<IServiceLevelIndicatorFeature>();
        ArgumentNullException.ThrowIfNull(feature);
        return feature.CustomerResourceId;
    }

    private static string GetOperation(HttpContext context, EndpointMetadataCollection metaData)
    {
        var attrib = GetSliAttribute(metaData);
        if (attrib is null || string.IsNullOrEmpty(attrib.Operation))
        {
            var description = metaData.GetMetadata<ControllerActionDescriptor>();
            return context.Request.Method + " " + description?.AttributeRouteInfo?.Template;
        }

        return attrib.Operation;
    }

    private void AddSliFeatureToHttpContext(HttpContext context)
    {
        if (context.Features.Get<IServiceLevelIndicatorFeature>() != null)
            throw new InvalidOperationException($"Another instance of {nameof(ServiceLevelIndicatorFeature)} already exists. Only one instance of {nameof(ServiceLevelIndicatorMiddleware)} can be configured for an application.");

        context.Features.Set<IServiceLevelIndicatorFeature>(new ServiceLevelIndicatorFeature(_serviceLevelIndicator.ServiceLevelIndicatorOptions.DefaultCustomerResourceId));
    }

    private static void RemoveSliFeatureFromHttpContext(HttpContext context) =>
        context.Features.Set<IServiceLevelIndicatorFeature?>(null);

}
