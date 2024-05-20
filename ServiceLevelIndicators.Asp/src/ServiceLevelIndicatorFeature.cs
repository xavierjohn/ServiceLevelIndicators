namespace ServiceLevelIndicators;

internal sealed class ServiceLevelIndicatorFeature : IServiceLevelIndicatorFeature
{
    public ServiceLevelIndicatorFeature(MeasuredOperation measureOperation) => MeasuredOperation = measureOperation;

    public MeasuredOperation MeasuredOperation { get; }
}
