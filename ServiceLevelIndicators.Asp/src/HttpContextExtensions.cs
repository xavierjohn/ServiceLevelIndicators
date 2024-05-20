namespace ServiceLevelIndicators;
using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;

public static class HttpContextExtensions
{
    /// <summary>
    /// Gets the MeasuredOperation from the IServiceLevelIndicatorFeature.
    /// The method will throw an exception if the route is not configured to emit SLI metrics.
    /// </summary>
    /// <param name="context"></param>
    /// <returns>MeasuredOperation for the current API method.</returns>
    /// <exception cref="ArgumentNullException"></exception>
    /// <exception cref="InvalidOperationException">If the route does not emit SLI information and therefore MeasuredOperation does not exist.</exception>
    public static MeasuredOperation GetMeasuredOperation(this HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        return context.Features.GetRequiredFeature<IServiceLevelIndicatorFeature>().MeasuredOperation;
    }

    /// <summary>
    /// Gets the MeasuredOperation from the IServiceLevelIndicatorFeature.
    /// </summary>
    /// <param name="context"></param>
    /// <param name="measuredOperation"></param>
    /// <returns>true if MeasuredOperation exists.</returns>
    /// <exception cref="ArgumentNullException"></exception>
    public static bool TryGetMeasuredOperation(this HttpContext context, [MaybeNullWhen(false)] out MeasuredOperation measuredOperation)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (context.Features.Get<IServiceLevelIndicatorFeature>() is IServiceLevelIndicatorFeature feature)
        {
            measuredOperation = feature.MeasuredOperation;
            return true;
        }

        measuredOperation = null;
        return false;
    }
}
