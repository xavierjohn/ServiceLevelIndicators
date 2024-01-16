namespace ServiceLevelIndicators;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

public interface IMeasuredOperationEnrichment
{
    ValueTask EnrichAsync(MeasuredOperationLatency measuredOperation, HttpContext httpContext);
}
