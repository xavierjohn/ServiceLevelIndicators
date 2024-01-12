namespace ServiceLevelIndicators.Asp;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using ServiceLevelIndicators;

public class EnrichMeasuredOperationLatency : IEnrichMeasuredOperationLatency
{
    public ValueTask EnrichMeasuredOperation(MeasuredOperationLatency measuredOperation, HttpContext httpContext) => ValueTask.CompletedTask;
}
