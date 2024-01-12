namespace ServiceLevelIndicators;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

public static class IServiceCollectionExtensions
{
    public static IServiceLevelIndicatorBuilder AddApiVersion(this IServiceLevelIndicatorBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<IMeasuredOperationEnrichment, ApiVersionEnrichment>());
        return builder;
    }
}
