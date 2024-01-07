namespace ServiceLevelIndicators;
using System;
using Microsoft.AspNetCore.Builder;

/// <summary>
/// Extension methods for adding the <see cref="ServiceLevelIndicators"/> to an application.
/// </summary>
public static class ServiceLevelIndicatorApplicationBuilderExtensions
{
    /// <summary>
    /// Adds the <see cref="ServiceLevelIndicatorMiddleware"/> for emitting SLI metrics.
    /// </summary>
    /// <param name="app">The <see cref="IApplicationBuilder"/>.</param>
    public static IApplicationBuilder UseServiceLevelIndicator(this IApplicationBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        return app.UseMiddleware<ServiceLevelIndicatorMiddleware>();
    }
}
