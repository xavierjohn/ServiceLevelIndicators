namespace ServiceLevelIndicators.Asp;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

public static class IServiceCollectionExtensions
{
    public static IServiceLevelIndicatorBuilder AddMvc(this IServiceLevelIndicatorBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.Services.AddMvcCore(static options => options.Conventions.Add(new ServiceLevelIndicatorConvention()));
        return builder;
    }
}
