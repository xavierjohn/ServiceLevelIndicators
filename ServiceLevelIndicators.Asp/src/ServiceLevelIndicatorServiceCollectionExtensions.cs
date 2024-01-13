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
        builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<IMeasuredOperationEnrichment, HttpMethodEnrichment>());
        return builder;
    }

    public static IServiceLevelIndicatorBuilder Enrich(this IServiceLevelIndicatorBuilder builder, Func<MeasuredOperationLatency, HttpContext, ValueTask> func)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(func);
        builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<IMeasuredOperationEnrichment>(new Enrich(func)));
        return builder;
    }
}

internal sealed class ServiceLevelIndicatorBuilder(IServiceCollection services) : IServiceLevelIndicatorBuilder
{
    public IServiceCollection Services => services;
}
