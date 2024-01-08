namespace ServiceLevelIndicators;

using Microsoft.Extensions.DependencyInjection;

public interface IServiceLevelIndicatorBuilder
{
    IServiceCollection Services { get; }
}
