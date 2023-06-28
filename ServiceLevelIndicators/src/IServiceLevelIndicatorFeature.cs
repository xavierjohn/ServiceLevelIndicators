namespace ServiceLevelIndicators;
/// <summary>
/// A feature for setting up service level indicators.
/// </summary>
public interface IServiceLevelIndicatorFeature
{
    MeasureOperationLatency MeasureOperationLatency { get; }
}
