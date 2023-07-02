namespace ServiceLevelIndicators;
using System;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;

public static class HttpContextExtensions
{
    public static MeasuredOperationLatency GetMeasuredOperationLatency(this HttpContext context)
    {
        if (context == null)
            throw new ArgumentNullException(nameof(context));

        return context.Features.GetRequiredFeature<IServiceLevelIndicatorFeature>().MeasuredOperationLatency;
    }
}
