namespace ServiceLevelIndicators;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

public static class ServiceLevelIndicatorServiceCollectionExtensions
{
    public static IServiceLevelIndicatorBuilder AddApiVersionEnrichment(this IServiceLevelIndicatorBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<IEnrichment<WebEnrichmentContext>, ApiVersionMeasurement>());
        return builder;
    }
}
