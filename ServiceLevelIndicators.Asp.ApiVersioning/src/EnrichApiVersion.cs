namespace ServiceLevelIndicators;
using System.Threading;
using System.Threading.Tasks;
using global::Asp.Versioning;
using Microsoft.AspNetCore.Http;

public class EnrichApiVersion : IEnrichMeasuredOperationLatency
{
    private readonly HttpContext _httpContext;

    public EnrichApiVersion(HttpContext httpContext) => _httpContext = httpContext;

    public ValueTask EnrichMeasuredOperation(MeasuredOperationLatency measuredOperation, CancellationToken cancellationToken)
    {
        measuredOperation.AddAttribute("http.api.version", GetApiVersion(_httpContext));
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
