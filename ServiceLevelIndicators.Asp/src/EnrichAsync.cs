namespace ServiceLevelIndicators;

using System.Threading;
using System.Threading.Tasks;

internal sealed class EnrichAsync : IMeasurement<WebMeasurementContext>
{
    private readonly Func<WebMeasurementContext, CancellationToken, ValueTask> _func;

    public EnrichAsync(Func<WebMeasurementContext, CancellationToken, ValueTask> func) => _func = func;

    ValueTask IMeasurement<WebMeasurementContext>.EnrichAsync(WebMeasurementContext context, CancellationToken cancellationToken)
        => _func(context, cancellationToken);
}
