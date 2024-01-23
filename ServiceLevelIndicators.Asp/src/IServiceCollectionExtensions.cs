namespace ServiceLevelIndicators;

using Microsoft.Extensions.DependencyInjection;

public static class IServiceCollectionExtensions
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

        services.AddSingleton<ServiceLevelIndicator>();
        services.Configure(configureOptions);

        return new ServiceLevelIndicatorBuilder(services);
    }
}
