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
        => context.GetMeasuredOperationLatency().SetApiVersion(GetApiVersion(context));

    private static string GetApiVersion(HttpContext context)
    {
        var versions = context.ApiVersioningFeature().RawRequestedApiVersions;
        if (versions.Count > 0)
            return string.Join(',', versions);
        else if (context.GetEndpoint()?.Metadata.GetMetadata<ApiVersionMetadata>() is { IsApiVersionNeutral: true })
            return "Neutral";

        return "Unspecified";
    }
}
