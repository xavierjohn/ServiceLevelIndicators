namespace ServiceLevelIndicators;

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
        var metadata = context.Features.Get<IEndpointFeature>()?.Endpoint?.Metadata;
        if (metadata == null || !ShouldEmitMetrics(metadata))
        {
            await _next(context);
            return;
        }

        var operation = GetOperation(context, metadata);
        using var measuredOperation = _serviceLevelIndicator.StartLatencyMeasureOperation(operation);
        AddSliFeatureToHttpContext(context, measuredOperation);
        await _next(context);
        UpdateOperationWithResponseStatus(context, measuredOperation);
        RemoveSliFeatureFromHttpContext(context);
    }

    private static void UpdateOperationWithResponseStatus(HttpContext context, MeasureOperationLatency measuredOperation)
    {
        var statusCode = context.Response.StatusCode;
        measuredOperation.SetHttpStatusCode(statusCode);
        measuredOperation.SetState((statusCode < StatusCodes.Status400BadRequest) ? System.Diagnostics.ActivityStatusCode.Ok : System.Diagnostics.ActivityStatusCode.Error);

        AddApiVersionIfPresent(context, measuredOperation);
    }

    private static void AddApiVersionIfPresent(HttpContext context, MeasureOperationLatency measuredOperation)
    {
        var version = GetApiVersion(context);
        if (!string.IsNullOrWhiteSpace(version))
            measuredOperation.SetApiVersion(version);
    }

    private bool ShouldEmitMetrics(EndpointMetadataCollection metadata) =>
        _serviceLevelIndicator.ServiceLevelIndicatorOptions.AutomaticallyEmitted || GetSliAttribute(metadata) is not null;

    private static ServiceLevelIndicatorAttribute? GetSliAttribute(EndpointMetadataCollection metaData) =>
        metaData.GetMetadata<ServiceLevelIndicatorAttribute>();

    private static string? GetApiVersion(HttpContext context) => context.ApiVersioningFeature().RawRequestedApiVersion;

    private static string GetOperation(HttpContext context, EndpointMetadataCollection metadata)
    {
        var attrib = GetSliAttribute(metadata);
        if (attrib is null || string.IsNullOrEmpty(attrib.Operation))
        {
            var description = metadata.GetMetadata<ControllerActionDescriptor>();
            return context.Request.Method + " " + description?.AttributeRouteInfo?.Template;
        }

        return attrib.Operation;
    }

    private void AddSliFeatureToHttpContext(HttpContext context, MeasureOperationLatency measuredOperation)
    {
        if (context.Features.Get<IServiceLevelIndicatorFeature>() != null)
            throw new InvalidOperationException($"Another instance of {nameof(ServiceLevelIndicatorFeature)} already exists. Only one instance of {nameof(ServiceLevelIndicatorMiddleware)} can be configured for an application.");

        context.Features.Set<IServiceLevelIndicatorFeature>(new ServiceLevelIndicatorFeature(measuredOperation));
    }

    private static void RemoveSliFeatureFromHttpContext(HttpContext context) =>
        context.Features.Set<IServiceLevelIndicatorFeature?>(null);

}
