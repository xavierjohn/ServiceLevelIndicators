namespace ServiceLevelIndicators;
using System.Threading.Tasks;

public interface IMeasurement<T>
    where T : IMeasurementContext
{
    ValueTask MeasureAsync(T context, CancellationToken cancellationToken);
}
