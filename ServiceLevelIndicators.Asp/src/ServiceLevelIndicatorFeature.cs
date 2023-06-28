namespace ServiceLevelIndicators;

internal sealed class ServiceLevelIndicatorFeature : IServiceLevelIndicatorFeature
{

    public ServiceLevelIndicatorFeature(MeasureOperationLatency measureOperationLatency) => MeasureOperationLatency = measureOperationLatency;

    public MeasureOperationLatency MeasureOperationLatency { get; }
}
