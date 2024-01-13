namespace ServiceLevelIndicators;

using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

internal sealed class EnrichAsync : IMeasuredOperationEnrichment
{
    private readonly Func<HttpContext, MeasuredOperationLatency, ValueTask> _func;

    public EnrichAsync(Func<HttpContext, MeasuredOperationLatency, ValueTask> func) => _func = func;

    ValueTask IMeasuredOperationEnrichment.EnrichAsync(MeasuredOperationLatency measuredOperation, HttpContext httpContext)
        => _func(httpContext, measuredOperation);
}
