namespace SampleVersionedWebApplicationSLI;

using System.Diagnostics.Metrics;

internal class SampleApiMeters
{
    public const string MeterName = "SampleMeter";

    public Meter Meter { get; } = new Meter(MeterName);
}
