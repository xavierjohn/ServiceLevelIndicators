namespace ServiceLevelIndicators;
public sealed class DefaultMeasurementContext(MeasuredOperationLatency measuredOperationLatency) : IMeasurementContext
{
    public void AddAttribute(string name, object value) => measuredOperationLatency.AddAttribute(name, value);
}
