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

        AddSliFeature(context);
        var operation = GetOperation(context, metaData);
        using var measuredOperation = _serviceLevelIndicator.StartLatencyMeasureOperation(operation);

        await _next(context);
        var statusCode = context.Response.StatusCode;
        measuredOperation.SetHttpStatusCode(statusCode);
        measuredOperation.SetState((statusCode >= 200 && statusCode < 300) ? System.Diagnostics.ActivityStatusCode.Ok : System.Diagnostics.ActivityStatusCode.Error);
        var customerResourceId = GetCustomerResourceId(context);
        measuredOperation.SetCustomerResourceId(customerResourceId);
    }

    private static string GetCustomerResourceId(HttpContext context)
    {
        var feature = context.Features.Get<IServiceLevelIndicatorFeature>();
        ArgumentNullException.ThrowIfNull(feature);
        return feature.CustomerResourceId;
    }

    private static string GetOperation(HttpContext context, EndpointMetadataCollection metaData)
    {
        var attrib = metaData.GetMetadata<ServiceLevelIndicatorAttribute>();
        if (attrib is null || attrib.Operation == string.Empty)
        {
            var description = metaData.GetMetadata<ControllerActionDescriptor>();
            return context.Request.Method + " " + description.AttributeRouteInfo.Template;
        }

        return attrib.Operation;
    }

    private void AddSliFeature(HttpContext context)
    {
        if (context.Features.Get<IServiceLevelIndicatorFeature>() != null)
            throw new InvalidOperationException($"Another instance of {nameof(ServiceLevelIndicatorFeature)} already exists. Only one instance of {nameof(ServiceLevelIndicatorMiddleware)} can be configured for an application.");

        context.Features.Set<IServiceLevelIndicatorFeature>(new ServiceLevelIndicatorFeature(_serviceLevelIndicator.ServiceLevelIndicatorOptions.DefaultCustomerResourceId));
    }
}
