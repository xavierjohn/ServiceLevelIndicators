namespace ServiceLevelIndicators;

using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

internal sealed class Enrich : IMeasuredOperationEnrichment
{
    private readonly Func<HttpContext, MeasuredOperationLatency, ValueTask> _func;

    public Enrich(Func<HttpContext, MeasuredOperationLatency, ValueTask> func) => _func = func;

    ValueTask IMeasuredOperationEnrichment.EnrichAsync(MeasuredOperationLatency measuredOperation, HttpContext httpContext)
        => _func(httpContext, measuredOperation);
}
