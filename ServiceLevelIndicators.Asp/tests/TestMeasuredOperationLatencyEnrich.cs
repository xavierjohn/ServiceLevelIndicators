namespace ServiceLevelIndicators.Asp.Tests;

using Microsoft.AspNetCore.Http;

internal class TestMeasuredOperationLatencyEnrich(string key, string value) : IEnrichMeasuredOperationLatency
{
    private readonly string _key = key;
    private readonly string _value = value;

    public ValueTask EnrichMeasuredOperation(MeasuredOperationLatency measuredOperation, HttpContext httpContext)
    {
        measuredOperation.AddAttribute(_key, _value);
        return ValueTask.CompletedTask;
    }
}
