namespace ServiceLevelIndicators;
using Microsoft.AspNetCore.Http;

public sealed class WebMeasurementContext : IMeasurementContext
{
    private readonly MeasuredOperationLatency _measuredOperationLatency;

    public WebMeasurementContext(HttpContext context)
    {
        _measuredOperationLatency = context.GetMeasuredOperationLatency();
        HttpContext = context;
    }

    public HttpContext HttpContext { get; }

    public void AddAttribute(string name, object value) => _measuredOperationLatency.AddAttribute(name, value);
}
