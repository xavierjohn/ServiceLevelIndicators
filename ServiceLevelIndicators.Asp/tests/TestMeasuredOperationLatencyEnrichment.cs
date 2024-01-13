namespace ServiceLevelIndicators.Asp.Tests;

using Microsoft.AspNetCore.Http;

internal class TestMeasuredOperationLatencyEnrichment(string key, string value) : IMeasuredOperationEnrichment
{
    private readonly string _key = key;
    private readonly string _value = value;

    public ValueTask EnrichAsync(MeasuredOperationLatency measuredOperation, HttpContext httpContext)
    {
        measuredOperation.AddAttribute(_key, _value);
        return ValueTask.CompletedTask;
    }
}
