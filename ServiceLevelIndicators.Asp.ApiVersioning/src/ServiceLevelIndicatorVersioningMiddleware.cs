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
        if (context.TryGetMeasuredOperationLatency(out var latency))
            latency.AddAttribute("http.api.version", GetApiVersion(context));
    }

    private static string GetApiVersion(HttpContext context)
    {
        var apiVersioningFeature = context.ApiVersioningFeature();
        var versions = apiVersioningFeature.RawRequestedApiVersions;
        if (versions.Count == 1)
            return apiVersioningFeature.RequestedApiVersion?.ToString() ?? string.Empty;
        if (versions.Count > 0)
            return string.Empty;
        if (context.GetEndpoint()?.Metadata.GetMetadata<ApiVersionMetadata>() is { IsApiVersionNeutral: true })
            return "Neutral";

        return "Unspecified";
    }
}
