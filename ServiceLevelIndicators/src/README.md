# ServiceLevelIndicators

[![NuGet Package](https://img.shields.io/nuget/v/ServiceLevelIndicators.svg)](https://www.nuget.org/packages/ServiceLevelIndicators)

A .NET library for emitting **Service Level Indicator (SLI)** latency metrics in milliseconds via the standard [System.Diagnostics.Metrics](https://learn.microsoft.com/dotnet/api/system.diagnostics.metrics) API. Use it to measure operation duration across any .NET application and attach dimensions such as operation name, customer, location, and outcome so the data is useful for SLO dashboards and alerts.

For ASP.NET Core applications, see [ServiceLevelIndicators.Asp](https://www.nuget.org/packages/ServiceLevelIndicators.Asp).

## When To Use This Package

Choose `ServiceLevelIndicators` when you want to measure operations directly in application code, especially in console apps, worker services, background jobs, libraries, or shared business logic that should stay independent from ASP.NET Core.

If you want automatic measurement of ASP.NET Core endpoints, use [ServiceLevelIndicators.Asp](https://www.nuget.org/packages/ServiceLevelIndicators.Asp) instead.

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

If you supply a custom `Meter` in `ServiceLevelIndicatorOptions`, register that same meter with OpenTelemetry:

```csharp
var sliMeter = new Meter("MyCompany.ServiceLevelIndicator");

builder.Services.AddOpenTelemetry()
    .WithMetrics(metrics =>
    {
        metrics.AddServiceLevelIndicatorInstrumentation(sliMeter);
        metrics.AddOtlpExporter();
    });

builder.Services.Configure<ServiceLevelIndicatorOptions>(options =>
{
    options.Meter = sliMeter;
    options.LocationId = ServiceLevelIndicator.CreateLocationId("public", "westus3");
    options.CustomerResourceId = "my-customer";
});

builder.Services.AddSingleton<ServiceLevelIndicator>();
```

### 2. Configure options

```csharp
builder.Services.Configure<ServiceLevelIndicatorOptions>(options =>
{
    options.LocationId = ServiceLevelIndicator.CreateLocationId("public", "westus3");
    options.CustomerResourceId = "my-customer";
});

builder.Services.AddSingleton<ServiceLevelIndicator>();
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

## Cardinality Guidance

Use stable, bounded values for `CustomerResourceId` and custom attributes. Good dimensions include tenant, subscription, environment, region, or product tier. Avoid highly variable values such as request IDs, email addresses, timestamps, or arbitrary user input unless high-cardinality metrics are intentional and supported by your backend.

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
- [Package selection and usage reference](https://github.com/xavierjohn/ServiceLevelIndicators/blob/main/docs/usage-reference.md)
- [OpenTelemetry .NET](https://opentelemetry.io/docs/languages/dotnet/)
