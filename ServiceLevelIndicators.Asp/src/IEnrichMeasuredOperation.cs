namespace ServiceLevelIndicators;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public interface IEnrichMeasuredOperationLatency
{
    ValueTask EnrichMeasuredOperation(MeasuredOperationLatency measuredOperation, CancellationToken cancellationToken);
}
