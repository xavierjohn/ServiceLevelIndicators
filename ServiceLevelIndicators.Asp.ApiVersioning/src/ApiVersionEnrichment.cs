namespace ServiceLevelIndicators;
using System.Threading.Tasks;
using global::Asp.Versioning;
using Microsoft.AspNetCore.Http;

public sealed class ApiVersionEnrichment : IMeasuredOperationEnrichment
{

    public ValueTask EnrichAsync(MeasuredOperationLatency measuredOperation, HttpContext httpContext)
    {
        measuredOperation.AddAttribute("http.api.version", GetApiVersion(httpContext));
        return ValueTask.CompletedTask;
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
