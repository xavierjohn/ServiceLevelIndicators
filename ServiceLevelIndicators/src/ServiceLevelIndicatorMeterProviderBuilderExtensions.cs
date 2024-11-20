namespace ServiceLevelIndicators;
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
    /// <param name="meterName"><see cref="string"/> if you are using customer meters, give that name.</param>
    /// <returns>The instance of <see cref="MeterProviderBuilder"/> to chain the calls.</returns>
    public static MeterProviderBuilder AddServiceLevelIndicatorInstrumentation(this MeterProviderBuilder builder, string? meterName = null)
        => builder.AddMeter(meterName ?? ServiceLevelIndicator.DefaultMeterName);
}
