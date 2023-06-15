namespace Asp.ServiceLevelIndicators;
using System.Diagnostics.Metrics;

public interface IServiceLevelIndicatorMeter
{
    /// <summary>
    /// The meter that is used to create the histogram that reports the latency.
    /// </summary>
    public Meter Meter { get; }
}
