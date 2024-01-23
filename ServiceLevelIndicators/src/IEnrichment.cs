namespace ServiceLevelIndicators;
using System.Threading.Tasks;

public interface IEnrichment<T>
    where T : IEnrichmentContext
{
    ValueTask EnrichAsync(T context, CancellationToken cancellationToken);
}
