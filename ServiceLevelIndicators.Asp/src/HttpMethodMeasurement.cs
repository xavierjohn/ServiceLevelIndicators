namespace ServiceLevelIndicators;
using System.Threading.Tasks;

internal sealed class HttpMethodMeasurement
    : IMeasurement<WebMeasurementContext>
{
    public ValueTask EnrichAsync(WebMeasurementContext context, CancellationToken cancellationToken)
    {
        context.AddAttribute("http.request.method", context.HttpContext.Request.Method);
        return ValueTask.CompletedTask;
    }
}
