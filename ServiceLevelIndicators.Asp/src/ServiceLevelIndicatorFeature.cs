namespace ServiceLevelIndicators;

internal sealed class ServiceLevelIndicatorFeature : IServiceLevelIndicatorFeature
{
    public ServiceLevelIndicatorFeature(MeasuredOperationLatency measureOperationLatency) => MeasuredOperationLatency = measureOperationLatency;

    public MeasuredOperationLatency MeasuredOperationLatency { get; }
}
