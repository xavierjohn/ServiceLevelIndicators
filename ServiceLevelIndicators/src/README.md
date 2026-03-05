# ServiceLevelIndicators

[![NuGet Package](https://img.shields.io/nuget/v/ServiceLevelIndicators.svg)](https://www.nuget.org/packages/ServiceLevelIndicators)

A .NET library for emitting **Service Level Indicator (SLI)** latency metrics via the standard [System.Diagnostics.Metrics](https://learn.microsoft.com/dotnet/api/system.diagnostics.metrics) API. Use it to measure operation duration across any .NET application — console apps, background services, worker processes, and more.

For ASP.NET Core applications, see [ServiceLevelIndicators.Asp](https://www.nuget.org/packages/ServiceLevelIndicators.Asp).

## Installation

```shell
dotnet add package ServiceLevelIndicators
```

## Quick Start

### 1. Register with OpenTelemetry

```csharp
builder.Services.AddOpenTelemetry()
    .WithMetrics(metrics =>
    {
        metrics.AddServiceLevelIndicatorInstrumentation();
        metrics.AddOtlpExporter();
    });
```

### 2. Configure options

```csharp
builder.Services.AddServiceLevelIndicator(options =>
{
    options.LocationId = ServiceLevelIndicator.CreateLocationId("public", "westus3");
    options.CustomerResourceId = "my-customer";
});
```

### 3. Measure operations

Wrap any block of code in a `using StartMeasuring` scope:

```csharp
async Task DoWork(ServiceLevelIndicator sli)
{
    using var op = sli.StartMeasuring("ProcessOrder");
    // Do work...
    op.SetActivityStatusCode(ActivityStatusCode.Ok);
}
```

You can also pass custom attributes:

```csharp
var attribute = new KeyValuePair<string, object?>("Event", "OrderCreated");
using var op = sli.StartMeasuring("ProcessOrder", attribute);
```

## Emitted Metrics

A meter named `ServiceLevelIndicator` with instrument `operation.duration` (milliseconds) is emitted with the following attributes:

| Attribute | Description |
|-----------|-------------|
| `Operation` | The name of the measured operation |
| `CustomerResourceId` | Identifies the customer, customer group, or calling service |
| `LocationId` | Where the service is running (e.g. `ms-loc://az/public/westus3`) |
| `activity.status.code` | `Ok`, `Error`, or `Unset` based on the operation outcome |

## Key APIs

| Type / Method | Description |
|---------------|-------------|
| `ServiceLevelIndicator.StartMeasuring(operation, attributes)` | Start a measured operation scope |
| `MeasuredOperation.SetActivityStatusCode(code)` | Set the outcome status |
| `MeasuredOperation.AddAttribute(name, value)` | Add a custom metric attribute |
| `MeasuredOperation.CustomerResourceId` | Get/set the customer resource ID |
| `ServiceLevelIndicator.CreateLocationId(cloud, region?, zone?)` | Helper to build a location ID string |
| `ServiceLevelIndicator.CreateCustomerResourceId(guid)` | Helper to build a customer resource ID from a service tree GUID |

## Further Reading

- [Full documentation and samples](https://github.com/xavierjohn/ServiceLevelIndicators)
- [OpenTelemetry .NET](https://opentelemetry.io/docs/languages/dotnet/)
