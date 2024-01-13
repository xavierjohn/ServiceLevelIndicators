namespace ServiceLevelIndicators;

using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

internal class Enrich : IMeasuredOperationEnrichment
{
    private readonly Func<MeasuredOperationLatency, HttpContext, ValueTask> _func;

    public Enrich(Func<MeasuredOperationLatency, HttpContext, ValueTask> func) => _func = func;

    ValueTask IMeasuredOperationEnrichment.Enrich(MeasuredOperationLatency measuredOperation, HttpContext httpContext)
        => _func(measuredOperation, httpContext);
}
