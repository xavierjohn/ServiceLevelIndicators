namespace Trellis.ServiceLevelIndicators;

using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;

internal sealed partial class ServiceLevelIndicatorMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ServiceLevelIndicator _serviceLevelIndicator;
    private readonly IEnumerable<IEnrichment<WebEnrichmentContext>> _enrichments;
    private readonly ILogger<ServiceLevelIndicatorMiddleware> _logger;

    public ServiceLevelIndicatorMiddleware(RequestDelegate next, ServiceLevelIndicator serviceLevelIndicator, IEnumerable<IEnrichment<WebEnrichmentContext>> enrichments, ILogger<ServiceLevelIndicatorMiddleware> logger)
    {
        _next = next;
        _serviceLevelIndicator = serviceLevelIndicator;
        _enrichments = enrichments;
        _logger = logger;
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

        using var measuredOperation = _serviceLevelIndicator.StartMeasuring(operation, attributes);
        SetCustomerResourceIdFromAttribute(context, metadata, measuredOperation);
        AddSliFeatureToHttpContext(context, measuredOperation);
        Exception? unhandledException = null;

        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            unhandledException = ex;

            if (!context.Response.HasStarted && context.Response.StatusCode < StatusCodes.Status500InternalServerError)
                context.Response.StatusCode = StatusCodes.Status500InternalServerError;

            throw;
        }
        finally
        {
            try
            {
                var webmeasurementContext = new WebEnrichmentContext(measuredOperation, context);
                UpdateOperationWithResponseStatus(context, measuredOperation, unhandledException is not null);

                foreach (var enrichment in _enrichments)
                {
                    if (context.RequestAborted.IsCancellationRequested) break;

                    try
                    {
                        await enrichment.EnrichAsync(webmeasurementContext, context.RequestAborted);
                    }
                    catch (Exception ex)
                    {
                        LogEnrichmentFailed(ex, enrichment.GetType().Name);
                    }
                }
            }
            finally
            {
                RemoveSliFeatureFromHttpContext(context);
            }
        }
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "SLI enrichment {EnrichmentType} failed.")]
    partial void LogEnrichmentFailed(Exception ex, string enrichmentType);

    [LoggerMessage(Level = LogLevel.Warning, Message = "SLI middleware could not resolve a route template for endpoint {Endpoint}; emitting bounded sentinel '<unrouted>' to avoid metric cardinality explosion. Set [ServiceLevelIndicator(Operation = \"...\")] or use a routed endpoint to fix this.")]
    partial void LogMissingRouteTemplate(string endpoint);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void SetCustomerResourceIdFromAttribute(HttpContext context, EndpointMetadataCollection metadata, MeasuredOperation measuredOperation)
    {
        var customerResourceId = GetCustomerResourceIdAttributes(context, metadata);
        if (customerResourceId is not null)
            measuredOperation.CustomerResourceId = customerResourceId;
    }

    private static void UpdateOperationWithResponseStatus(HttpContext context, MeasuredOperation measuredOperation, bool unhandledException = false)
    {
        var statusCode = context.Response.StatusCode;
        measuredOperation.AddAttribute("http.response.status.code", statusCode);
        var activityCode = unhandledException ? ActivityStatusCode.Error : statusCode switch
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

    private string GetOperation(HttpContext context, EndpointMetadataCollection metadata)
    {
        var sli = GetSliAttribute(metadata);
        if (sli is not null && !string.IsNullOrEmpty(sli.Operation))
            return sli.Operation;

        // Tier 1: MVC controller with attribute routing.
        var template = metadata.GetMetadata<ControllerActionDescriptor>()?.AttributeRouteInfo?.Template;

        // Tier 2: Minimal APIs / conventional routing — pull the route pattern (with placeholders) from the endpoint.
        if (string.IsNullOrEmpty(template))
            template = (context.GetEndpoint() as RouteEndpoint)?.RoutePattern.RawText;

        // Tier 3: No bounded template available. Emit a sentinel so the Operation dimension stays bounded by
        // (HTTP method count) x (endpoint count) rather than exploding to one series per concrete request path.
        if (string.IsNullOrEmpty(template))
        {
            LogMissingRouteTemplate(context.GetEndpoint()?.DisplayName ?? "(no endpoint)");
            return context.Request.Method + " <unrouted>";
        }

        return context.Request.Method + " " + template;
    }

    private static string? GetCustomerResourceIdAttributes(HttpContext context, EndpointMetadataCollection metadata)
    {
        var measure = metadata.GetMetadata<CustomerResourceIdMetadata>();
        if (measure is null)
            return null;

        var values = context.Request.RouteValues;
        var value = values.TryGetValue(measure.RouteValueName, out var val) ? val : default;
        return value?.ToString();
    }

    private static KeyValuePair<string, object?>[] GetMeasuredAttributes(HttpContext context, EndpointMetadataCollection metadata)
    {
        var measures = metadata.GetOrderedMetadata<MeasureMetadata>();
        var count = measures.Count;

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

    private void AddSliFeatureToHttpContext(HttpContext context, MeasuredOperation measuredOperation)
    {
        if (context.Features.Get<IServiceLevelIndicatorFeature>() != null)
            throw new InvalidOperationException($"Another instance of {nameof(ServiceLevelIndicatorFeature)} already exists. Only one instance of {nameof(ServiceLevelIndicatorMiddleware)} can be configured for an application.");

        context.Features.Set<IServiceLevelIndicatorFeature>(new ServiceLevelIndicatorFeature(measuredOperation));
    }

    private static void RemoveSliFeatureFromHttpContext(HttpContext context) =>
        context.Features.Set<IServiceLevelIndicatorFeature?>(null);

}