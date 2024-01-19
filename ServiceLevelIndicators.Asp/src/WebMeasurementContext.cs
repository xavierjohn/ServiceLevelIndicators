namespace ServiceLevelIndicators;
using Microsoft.AspNetCore.Http;

public class WebMeasurementContext : IMeasurementContext
{
    private readonly MeasuredOperationLatency _operation;
    public HttpContext HttpContext { get; }

    public WebMeasurementContext(MeasuredOperationLatency operation, HttpContext httpContext)
    {
        _operation = operation;
        HttpContext = httpContext;
    }
    public string Operation => _operation.Operation;


    public void AddAttribute(string name, object value) => _operation.AddAttribute(name, value);

    public void SetCustomerResourceId(string id) => _operation.CustomerResourceId = id;
}
