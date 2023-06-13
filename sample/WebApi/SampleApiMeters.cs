namespace SampleWebApplicationSLI
{
    using System.Diagnostics.Metrics;

    public class SampleApiMeters
    {
        public const string MeterName = "SampleMeter";

        public Meter Meter { get; } = new Meter(MeterName);
    }
}
