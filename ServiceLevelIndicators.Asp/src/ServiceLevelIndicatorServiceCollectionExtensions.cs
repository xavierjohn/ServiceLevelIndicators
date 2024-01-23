namespace ServiceLevelIndicators;

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

/// <summary>
/// Extension methods for the ServiceLevelIndicator middleware.
/// </summary>
public static class ServiceLevelIndicatorServiceCollectionExtensions
{
    public static IServiceLevelIndicatorBuilder AddMvc(this IServiceLevelIndicatorBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.Services.AddMvcCore(static options => options.Conventions.Add(new ServiceLevelIndicatorConvention()));
        return builder;
    }

    public static IServiceLevelIndicatorBuilder AddHttpMethodEnrichment(this IServiceLevelIndicatorBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<IEnrichment<WebEnrichmentContext>, HttpMethodEnrichment>());
        return builder;
    }

    public static IServiceLevelIndicatorBuilder Enrich(this IServiceLevelIndicatorBuilder builder, Action<WebEnrichmentContext> action)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(action);
        builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<IEnrichment<WebEnrichmentContext>>(new Enrich(action)));
        return builder;
    }

    public static IServiceLevelIndicatorBuilder EnrichAsync(this IServiceLevelIndicatorBuilder builder, Func<WebEnrichmentContext, CancellationToken, ValueTask> func)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(func);
        builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<IEnrichment<WebEnrichmentContext>>(new EnrichAsync(func)));
        return builder;
    }
}

internal sealed class ServiceLevelIndicatorBuilder(IServiceCollection services) : IServiceLevelIndicatorBuilder
{
    public IServiceCollection Services => services;
}
