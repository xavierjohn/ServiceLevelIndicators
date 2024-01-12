﻿namespace ServiceLevelIndicators;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

public interface IEnrichMeasuredOperationLatency
{
    ValueTask EnrichMeasuredOperation(MeasuredOperationLatency measuredOperation, HttpContext httpContext);
}
