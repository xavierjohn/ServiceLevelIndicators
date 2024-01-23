namespace ServiceLevelIndicators.Asp.Tests;

using System.Threading;

internal class TestEnrichment(string key, string value)
    : IEnrichment<WebEnrichmentContext>
{
    private readonly string _key = key;
    private readonly string _value = value;

    public ValueTask EnrichAsync(WebEnrichmentContext context, CancellationToken cancellationToken)
    {
        context.AddAttribute(_key, _value);
        return ValueTask.CompletedTask;
    }
}
