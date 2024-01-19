namespace ServiceLevelIndicators;
using System.Threading;
using System.Threading.Tasks;

internal sealed class Enrich : IMeasurement<WebMeasurementContext>
{
    private readonly Action<WebMeasurementContext> _action;

    public Enrich(Action<WebMeasurementContext> func) => _action = func;

    public ValueTask EnrichAsync(WebMeasurementContext context, CancellationToken cancellationToken)
    {
        _action(context);
        return ValueTask.CompletedTask;
    }
}
