namespace ServiceLevelIndicators;

using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Asp.Versioning;

internal sealed class ServiceLevelIndicatorVersioningMiddleware
{
    private readonly RequestDelegate _next;

    public ServiceLevelIndicatorVersioningMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(HttpContext context)
    {
        await _next(context);
        AddApiVersionDimensionToSli(context);
    }

    private static void AddApiVersionDimensionToSli(HttpContext context)
    {
        var apiVersionfeature = context.ApiVersioningFeature();
        if (apiVersionfeature == null) return;

        var measuredOperationLatency = context.GetMeasuredOperationLatency();
        var version = "Unknown";
        var versions = apiVersionfeature.RawRequestedApiVersions;
        if (versions.Count > 0)
            version = string.Join(',', versions);
        else
        {
            var metadata = context.GetEndpoint()?.Metadata.GetMetadata<ApiVersionMetadata>();
            if (metadata != null && metadata.IsApiVersionNeutral)
                version = "Neutral";
        }
        measuredOperationLatency.SetApiVersion(version);
    }
}
