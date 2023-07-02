---
ArtifactType: nupkg.
Language: csharp.
Tags: SLI, OpenTelemetry, Metrics.
---

# Service Level Indicators

Service Level Indicator library will help emit latency metrics for each API operation. The metrics is emitted via OpenTelemetry and can be used to monitor service level agreements.

By default an instrument named `LatencySLI` is added to the service metrics and the metrics are emitted. The metrics are emitted with the following [attributes](https://opentelemetry.io/docs/specs/otel/common/#attribute).

* CustomerResourceId - The customer resource id.
* LocationId - The location id. Where is the service running? eg. Public cloud, West US 3 region.
* Operation - The name of the operation. Defaults to `AttributeRouteInfo.Template` information.
* HttpStatusCode - The http status code.
* api_version - If [API Versioning](https://github.com/dotnet/aspnet-api-versioning) is used, the version of the API.

## Prerequisites

The library targets .net core and requires the service to use OpenTelemetry https://opentelemetry.io/.

1. Create and register a metrics meter with the dependency injection.

   Example.

    ``` csharp
    public class SampleApiMeters
    {
        public const string MeterName = "SampleMeter";
        public Meter Meter { get; } = new Meter(MeterName);
    }
    builder.Services.AddSingleton<SampleApiMeters>();
    ```

2. Add ServiceLevelIndicator into the dependency injection.

   Example.

    ``` csharp
    builder.Services.AddSingleton<IServiceLevelIndicatorMeter>(sp =>
    {
        var meters = sp.GetRequiredService<SampleApiMeters>();
        return new ServiceLevelIndicatorMeter(meters.Meter);
    });
    builder.Services.AddServiceLevelIndicator(options =>
    {
        options.CustomerResourceId = ServiceLevelIndicator.CreateCustomerResourceId(serviceId);
        options.LocationId = ServiceLevelIndicator.CreateLocationId("Public", "eastus2");
    });

     ```

### Usage

Once the Prerequisites are done, all controllers will emit SLI information.
The default operation name is in the format HTTP Method_Controller_Action. 
eg GET WeatherForecast_Get_WeatherForecast/Action1

1. To override the default operation name add the attribute `[ServiceLevelIndicator]` on the method.

   Example.

    ``` csharp
    [HttpGet("MyAction2")]
    [ServiceLevelIndicator(Operation = "MyOperation")]
    public IEnumerable<WeatherForecast> GetOperation() => GetWeather();
    ```

2. To set the CustomerResourceId within an API method, get the `IServiceLevelIndicatorFeature` and set it.

    ``` csharp
    [HttpGet("{customerResourceId}")]
    public IEnumerable<WeatherForecast> Get(string customerResourceId)
    {
        var sliFeature = HttpContext.Features.GetRequiredFeature<IServiceLevelIndicatorFeature>();
        sliFeature.MeasureOperationLatency.CustomerResourceId = customerResourceId;
        return GetWeather();
    }
    ```
    Or use `GetMeasuredOperationLatency` extension method.
        
    ``` csharp
    [HttpGet("{customerResourceId}")]
    public IEnumerable<WeatherForecast> Get(string customerResourceId)
    {
        HttpContext.GetMeasuredOperationLatency().CustomerResourceId = customerResourceId;
        return GetWeather();
    }
    ```
or mark the parameter with the attribute `[CustomerResourceId]`
```csharp
    [HttpGet("get-by-zip-code/{zipCode}")]
    public IEnumerable<WeatherForecast> GetByZipcode([CustomerResourceId] string zipCode) => GetWeather();
```

3. To add custom Open Telemetry attributes.
    ``` csharp 
        HttpContext.GetMeasuredOperationLatency().AddAttribute(attribute, value);
    ```
    
4. To measure a process, run it withing a `StartLatencyMeasureOperation` using block.

   Example.

    ``` csharp
   public override async Task StoreItem(CaseEvent domainEvent, CancellationToken cancellationToken)
    {
        var attribute = new KeyValuePair<string, object?>("Event", domainEvent.GetType().Name);
        using var measuredOperation = _serviceLevelIndicator.StartLatencyMeasureOperation("StoreItem", attribute);
        DoTheWork();
    ```

### Sample

Try out the sample weather forecast Web API.

To view the metrics locally.

1. Run Docker Desktop
2. Run [sample\DockerOpenTelemetry\run.cmd](sample\DockerOpenTelemetry\run.cmd) to download and run zipkin and prometheus.
3. Run the sample web API project and call the `GET WeatherForecast` using the Open API UI.
4. You should see the SLI metrics in prometheus under the meter `LatencySLI_bucket` where the `Operation = "GET WeatherForeCase"`, `HttpStatusCode = 200`, `LocationId = "public_West US 3"`, `Status = Ok`
![SLI](assets/prometheus.jpg)
5. If you run the sample with API Versioning, you will see something similar to the following.
![SLI](assets/versioned.jpg)