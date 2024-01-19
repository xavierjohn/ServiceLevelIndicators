namespace ServiceLevelIndicators;
using System.Threading.Tasks;

public interface IMeasurement<T>
    where T : IMeasurementContext
{
    ValueTask EnrichAsync(T context, CancellationToken cancellationToken);
}
