namespace ServiceLevelIndicators;

using Microsoft.Extensions.DependencyInjection;
using ServiceLevelIndicators.Asp;

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
    public static IServiceLevelIndicatorBuilder AddServiceLevelIndicator(this IServiceCollection services, Action<ServiceLevelIndicatorOptions> configureOptions)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configureOptions);

        services.AddSingleton<IMeasuredOperationEnrichment, EnrichMeasuredOperationLatency>();
        services.AddSingleton<ServiceLevelIndicator>();
        services.Configure(configureOptions);

        return new ServiceLevelIndicatorBuilder(services);
    }
}

internal sealed class ServiceLevelIndicatorBuilder(IServiceCollection services) : IServiceLevelIndicatorBuilder
{
    public IServiceCollection Services => services;
}
