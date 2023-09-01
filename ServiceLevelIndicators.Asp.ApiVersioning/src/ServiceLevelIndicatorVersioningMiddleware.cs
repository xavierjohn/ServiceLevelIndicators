namespace ServiceLevelIndicators;

using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

internal sealed class ServiceLevelIndicatorVersioningMiddleware
{
    private readonly RequestDelegate _next;

    public ServiceLevelIndicatorVersioningMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(HttpContext context)
    {
        await _next(context);
        var apiVersionfeature = context.ApiVersioningFeature();
        var slifeature = context.Features.Get<IServiceLevelIndicatorFeature>();

        if (apiVersionfeature != null && slifeature != null)
        {
            var apiFeature = context.ApiVersioningFeature();
            var versions = apiFeature.RawRequestedApiVersions;
            if (versions.Count > 0)
            {
                var version = string.Join(',', versions);
                slifeature.MeasuredOperationLatency.SetApiVersion(version);
            }
        }
    }
}
