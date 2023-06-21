namespace ServiceLevelIndicators;
using System.Diagnostics.Metrics;

public class ServiceLevelIndicatorMeter : IServiceLevelIndicatorMeter
{
    public ServiceLevelIndicatorMeter(Meter meter) => Meter = meter;

    public Meter Meter { get; }
}
