namespace ServiceLevelIndicators;

using System.Threading;
using System.Threading.Tasks;

internal sealed class EnrichAsync : IEnrichment<WebEnrichmentContext>
{
    private readonly Func<WebEnrichmentContext, CancellationToken, ValueTask> _func;

    public EnrichAsync(Func<WebEnrichmentContext, CancellationToken, ValueTask> func) => _func = func;

    ValueTask IEnrichment<WebEnrichmentContext>.EnrichAsync(WebEnrichmentContext context, CancellationToken cancellationToken)
        => _func(context, cancellationToken);
}
