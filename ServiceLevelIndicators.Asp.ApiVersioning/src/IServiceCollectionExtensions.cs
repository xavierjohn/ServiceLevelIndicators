namespace ServiceLevelIndicators;

using Microsoft.Extensions.DependencyInjection;

public static class IServiceCollectionExtensions
{
    public static IServiceLevelIndicatorBuilder AddApiVersion(this IServiceLevelIndicatorBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.Services.AddSingleton<IMeasuredOperationEnrichment, EnrichApiVersion>();
        return builder;
    }
}
