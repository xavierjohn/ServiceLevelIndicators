﻿namespace ServiceLevelIndicators;
using System;
using Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Extension methods for the ServiceLevelIndicator middleware.
/// </summary>
public static class ServiceLevelIndicatorServiceCollectionExtensions
{
    /// <summary>
    /// Add service level indicator options.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> for adding services.</param>
    /// <param name="configureOptions">A delegate to configure the <see cref="ServiceLevelIndicatorOptions"/>.</param>
    /// <returns></returns>
    public static IServiceCollection AddServiceLevelIndicator(this IServiceCollection services, Action<ServiceLevelIndicatorOptions> configureOptions)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configureOptions);

        services.AddSingleton<ServiceLevelIndicator>();
        services.Configure(configureOptions);
        return services;
    }
}
