namespace ServiceLevelIndicators;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

public interface IMeasuredOperationEnrichment
{
    ValueTask EnrichMeasuredOperation(MeasuredOperationLatency measuredOperation, HttpContext httpContext);
}
