namespace Trellis.ServiceLevelIndicators;

using Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Extension methods for registering Service Level Indicator services.
/// </summary>
public static class ServiceLevelIndicatorCoreServiceCollectionExtensions
{
    /// <summary>
    /// Adds Service Level Indicator services and options.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configureOptions">A delegate to configure <see cref="ServiceLevelIndicatorOptions"/>.</param>
    /// <returns>A builder for chaining host-specific SLI integrations.</returns>
    public static IServiceLevelIndicatorBuilder AddServiceLevelIndicator(this IServiceCollection services, Action<ServiceLevelIndicatorOptions> configureOptions)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configureOptions);

        services.AddSingleton<ServiceLevelIndicator>();
        services.Configure(configureOptions);

        return new ServiceLevelIndicatorBuilder(services);
    }
}
