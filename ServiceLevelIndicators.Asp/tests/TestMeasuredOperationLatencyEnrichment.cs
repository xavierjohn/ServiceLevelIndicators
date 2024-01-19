namespace ServiceLevelIndicators.Asp.Tests;

using System.Threading;

internal class TestMeasuredOperationLatencyEnrichment(string key, string value)
    : IMeasurement<WebMeasurementContext>
{
    private readonly string _key = key;
    private readonly string _value = value;

    public ValueTask EnrichAsync(WebMeasurementContext context, CancellationToken cancellationToken)
    {
        context.AddAttribute(_key, _value);
        return ValueTask.CompletedTask;
    }
}
