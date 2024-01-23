namespace ServiceLevelIndicators;
using System.Threading;
using System.Threading.Tasks;

internal sealed class Enrich : IEnrichment<WebEnrichmentContext>
{
    private readonly Action<WebEnrichmentContext> _action;

    public Enrich(Action<WebEnrichmentContext> func) => _action = func;

    public ValueTask EnrichAsync(WebEnrichmentContext context, CancellationToken cancellationToken)
    {
        _action(context);
        return ValueTask.CompletedTask;
    }
}
