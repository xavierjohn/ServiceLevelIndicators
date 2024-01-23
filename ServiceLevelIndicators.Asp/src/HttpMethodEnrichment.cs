namespace ServiceLevelIndicators;
using System.Threading.Tasks;

internal sealed class HttpMethodEnrichment
    : IEnrichment<WebEnrichmentContext>
{
    public ValueTask EnrichAsync(WebEnrichmentContext context, CancellationToken cancellationToken)
    {
        context.AddAttribute("http.request.method", context.HttpContext.Request.Method);
        return ValueTask.CompletedTask;
    }
}
