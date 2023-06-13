---
ArtifactType: nupkg.
Language: csharp.
Tags: SLI, OpenTelemetry, Metrics.
---

# Service Level Indicators

Service Level Indicator library will help emit latency metrics for each API operation. The metrics is emitted via OpenTelemetry and can be used to monitor service level agreements.

By default a meter named `LatencySLI` is created and the metrics are emitted to the meter. The metrics are emitted with the following dimensions (Tags).

* CustomerResourceId - The customer resource id.
* LocationId - The location id. Where is the service running? eg. public cloud, West US 3 region.
* Operation - The name of the operation.
* HttpStatusCode - The http status code.


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
    builder.Services.AddSingleton((sp) =>
    {
        var meters = sp.GetRequiredService<SampleApiMeters>();

        var customerResourceId = ServiceLevelIndicator.CreateCustomerResourceId("MyProduct", "SampleAPI");
        var locationId = ServiceLevelIndicator.CreateLocationId("public", "West US 3");
        return new ServiceLevelIndicator(customerResourceId, locationId, meters.Meter);
    });

    ```

### Usage

Once the Prerequisites are done, it is ready for usage.

1. To measure all the operation in a controller add the attribute `[ServiceLevelIndicatorLatency]`.

   Example.

    ``` csharp
    [ApiController]
    [Route("[controller]")]
    [ServiceLevelIndicatorLatency]
    public class WeatherForecastController : ControllerBase
    ```

2. To measure a process, run it withing a `StartLatencyMeasureOperation` using block.

   Example.

    ``` csharp
   public override async Task StoreItem(CaseEvent domainEvent, CancellationToken cancellationToken)
    {
        var tags = new KeyValuePair<string, object?>("Event", domainEvent.GetType().Name);
        using var measuredOperation = _serviceLevelIndicator.StartLatencyMeasureOperation("StoreItem", tags);
        DoTheWork();
    ```

### Sample

Try out the sample weather forecast Web API.

To view the metrics locally.

1. Run Docker Desktop
2. Run [sample\DockerOpenTelemetry\run.cmd](sample\DockerOpenTelemetry\run.cmd) to download and run zipkin and prometheus.
3. Run the sample web API project and call the `GET WeatherForecast` using the Open API UI.
4. You should see the SLI metrics in prometheus under the meter `LatencySLI_bucket` where the `Operation = "GET WeatherForecase"`, `HttpStatusCode = 200`, `LocationId = "public_West US 3"`, `Status = Ok`
![SLI](assets/prometheus.jpg)