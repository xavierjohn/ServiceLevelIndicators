namespace ServiceLevelIndicators;

using System.Threading.Tasks;
using Asp.Versioning;
using Microsoft.AspNetCore.Http;

internal sealed class ServiceLevelIndicatorVersioningMiddleware
{
    private readonly RequestDelegate _next;

    public ServiceLevelIndicatorVersioningMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(HttpContext context)
    {
        var apiVersionfeature = context.Features.Get<IApiVersioningFeature>();
        var slifeature = context.Features.Get<IServiceLevelIndicatorFeature>();

        if (apiVersionfeature != null && slifeature != null)
        {
            var version = context.ApiVersioningFeature().RawRequestedApiVersion;
            if (!string.IsNullOrWhiteSpace(version))
                slifeature.MeasuredOperationLatency.SetApiVersion(version);
        }
        await _next(context);
    }
}
