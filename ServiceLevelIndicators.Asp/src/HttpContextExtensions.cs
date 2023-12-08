namespace ServiceLevelIndicators;
using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;

public static class HttpContextExtensions
{
    /// <summary>
    /// Gets the MeasuredOperationLatency from the IServiceLevelIndicatorFeature.
    /// The method will throw an exception if the route is not configured to emit SLI metrics.
    /// </summary>
    /// <param name="context"></param>
    /// <returns>MeasuredOperationLatency for the current API method.</returns>
    /// <exception cref="ArgumentNullException"></exception>
    /// <exception cref="InvalidOperationException">If the route does not emit SLI information and therefore MeasuredOperationLatency does not exist.</exception>
    public static MeasuredOperationLatency GetMeasuredOperationLatency(this HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        return context.Features.GetRequiredFeature<IServiceLevelIndicatorFeature>().MeasuredOperationLatency;
    }

    /// <summary>
    /// Gets the MeasuredOperationLatency from the IServiceLevelIndicatorFeature.
    /// </summary>
    /// <param name="context"></param>
    /// <param name="measuredOperationLatency"></param>
    /// <returns>true if MeasuredOperationLatency exists.</returns>
    /// <exception cref="ArgumentNullException"></exception>
    public static bool TryGetMeasuredOperationLatency(this HttpContext context, [MaybeNullWhen(false)] out MeasuredOperationLatency measuredOperationLatency)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (context.Features.Get<IServiceLevelIndicatorFeature>() is IServiceLevelIndicatorFeature feature)
        {
            measuredOperationLatency = feature.MeasuredOperationLatency;
            return true;
        }

        measuredOperationLatency = null;
        return false;
    }
}
