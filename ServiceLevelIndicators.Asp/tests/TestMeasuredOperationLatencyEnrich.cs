namespace ServiceLevelIndicators.Asp.Tests;
internal class TestMeasuredOperationLatencyEnrich(string key, string value) : IEnrichMeasuredOperationLatency
{
    private readonly string _key = key;
    private readonly string _value = value;

    public void EnrichMeasuredOperation(MeasuredOperationLatency measuredOperation)
    {
        measuredOperation.AddAttribute(_key, _value);
    }
}
