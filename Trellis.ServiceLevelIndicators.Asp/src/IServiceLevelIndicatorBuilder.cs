namespace Trellis.ServiceLevelIndicators;

using Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Builder interface for configuring Service Level Indicator services.
/// </summary>
public interface IServiceLevelIndicatorBuilder
{
    /// <summary>
    /// Gets the <see cref="IServiceCollection"/> where SLI services are registered.
    /// </summary>
    IServiceCollection Services { get; }
}