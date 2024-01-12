namespace ServiceLevelIndicators;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

public interface IMeasuredOperationEnrichment
{
    ValueTask Enrich(MeasuredOperationLatency measuredOperation, HttpContext httpContext);
}
