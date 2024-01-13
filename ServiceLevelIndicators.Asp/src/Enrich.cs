namespace ServiceLevelIndicators;

using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

internal sealed class Enrich : IMeasuredOperationEnrichment
{
    private readonly Action<HttpContext, MeasuredOperationLatency> _action;

    public Enrich(Action<HttpContext, MeasuredOperationLatency> func) => _action = func;

    ValueTask IMeasuredOperationEnrichment.EnrichAsync(MeasuredOperationLatency measuredOperation, HttpContext httpContext)
    {
        _action(httpContext, measuredOperation);
        return ValueTask.CompletedTask;
    }
}
