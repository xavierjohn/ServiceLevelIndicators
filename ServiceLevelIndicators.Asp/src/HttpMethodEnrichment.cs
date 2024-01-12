namespace ServiceLevelIndicators;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

internal sealed class HttpMethodEnrichment : IMeasuredOperationEnrichment
{
    public ValueTask Enrich(MeasuredOperationLatency measuredOperation, HttpContext context)
    {
        measuredOperation.AddAttribute("http.request.method", context.Request.Method);
        return ValueTask.CompletedTask;
    }
}
