namespace ServiceLevelIndicators;

internal sealed class ServiceLevelIndicatorFeature : IServiceLevelIndicatorFeature
{

    public ServiceLevelIndicatorFeature(MeasuredOperationLatency measureOperationLatency) => MeasureOperationLatency = measureOperationLatency;

    public MeasuredOperationLatency MeasureOperationLatency { get; }
}
