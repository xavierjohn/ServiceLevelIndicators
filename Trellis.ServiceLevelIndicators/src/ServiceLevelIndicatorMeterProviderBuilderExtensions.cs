namespace Trellis.ServiceLevelIndicators;

using System;
using System.Diagnostics.Metrics;
using OpenTelemetry.Metrics;

/// <summary>
/// Extension methods to simplify registering of Service Level Indicator instrumentation.
/// </summary>
public static class ServiceLevelIndicatorMeterProviderBuilderExtensions
{
    /// <summary>
    /// Enables Service Level Indicator instrumentation.
    /// </summary>
    /// <param name="builder"><see cref="MeterProviderBuilder"/> being configured.</param>
    /// <returns>The instance of <see cref="MeterProviderBuilder"/> to chain the calls.</returns>
    public static MeterProviderBuilder AddServiceLevelIndicatorInstrumentation(this MeterProviderBuilder builder)
        => builder.AddServiceLevelIndicatorInstrumentation(ServiceLevelIndicator.DefaultMeterName);

    /// <summary>
    /// Enables Service Level Indicator instrumentation for a specific meter name.
    /// </summary>
    /// <param name="builder"><see cref="MeterProviderBuilder"/> being configured.</param>
    /// <param name="meterName">The name of the meter that emits Service Level Indicator metrics.</param>
    /// <returns>The instance of <see cref="MeterProviderBuilder"/> to chain the calls.</returns>
    public static MeterProviderBuilder AddServiceLevelIndicatorInstrumentation(this MeterProviderBuilder builder, string meterName)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentException.ThrowIfNullOrWhiteSpace(meterName);

        return builder.AddMeter(meterName);
    }

    /// <summary>
    /// Enables Service Level Indicator instrumentation for a specific meter instance.
    /// </summary>
    /// <param name="builder"><see cref="MeterProviderBuilder"/> being configured.</param>
    /// <param name="meter">The meter that emits Service Level Indicator metrics.</param>
    /// <returns>The instance of <see cref="MeterProviderBuilder"/> to chain the calls.</returns>
    public static MeterProviderBuilder AddServiceLevelIndicatorInstrumentation(this MeterProviderBuilder builder, Meter meter)
    {
        ArgumentNullException.ThrowIfNull(meter);
        return builder.AddServiceLevelIndicatorInstrumentation(meter.Name);
    }
}