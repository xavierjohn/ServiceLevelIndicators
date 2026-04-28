# Trellis.ServiceLevelIndicators

[![NuGet Package](https://img.shields.io/nuget/v/Trellis.ServiceLevelIndicators.svg)](https://www.nuget.org/packages/Trellis.ServiceLevelIndicators)

A .NET library for emitting **Service Level Indicator (SLI)** latency metrics in milliseconds via the standard [System.Diagnostics.Metrics](https://learn.microsoft.com/dotnet/api/system.diagnostics.metrics) API. Use it to measure operation duration across any .NET application and attach dimensions such as operation name, customer, location, and outcome so the data is useful for SLO dashboards and alerts.

For ASP.NET Core applications, see [Trellis.ServiceLevelIndicators.Asp](https://www.nuget.org/packages/Trellis.ServiceLevelIndicators.Asp).

## When To Use This Package

Choose `Trellis.ServiceLevelIndicators` when you want to measure operations directly in application code, especially in console apps, worker services, background jobs, libraries, or shared business logic that should stay independent from ASP.NET Core.

If you want automatic measurement of ASP.NET Core endpoints, use [Trellis.ServiceLevelIndicators.Asp](https://www.nuget.org/packages/Trellis.ServiceLevelIndicators.Asp) instead.

## Installation

```shell
dotnet add package Trellis.ServiceLevelIndicators
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

builder.Services.AddServiceLevelIndicator(options =>
{
    options.Meter = sliMeter;
    options.LocationId = ServiceLevelIndicator.CreateLocationId("public", "westus3");
    options.CustomerResourceId = "my-customer";
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
void DoWork(ServiceLevelIndicator sli)
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

Custom attribute names must be unique and must not reuse SLI-reserved tags such as `CustomerResourceId`, `LocationId`, `Operation`, or `activity.status.code`.

## Emitted Metrics

By default, a meter named `Trellis.SLI` emits the `operation.duration` histogram in milliseconds. If you configure `ServiceLevelIndicatorOptions.Meter`, metrics are emitted from that meter instead.

`StartMeasuring(...)` emits the following attributes when the returned `MeasuredOperation` is disposed:

| Attribute | Description |
|-----------|-------------|
| `Operation` | The name of the measured operation |
| `CustomerResourceId` | A **stable** identifier for the entity the operation is acting on (tenant, subscription, account, work item, etc.). NOT the caller, NOT a per-request ID, NOT a user/email. |
| `LocationId` | Where the service is running (e.g. `ms-loc://az/public/westus3`) |
| `activity.status.code` | `Ok`, `Error`, or `Unset` based on the operation outcome |

Direct `Record(...)` calls emit `CustomerResourceId`, `LocationId`, `Operation`, and any custom attributes supplied to the call; they do not add `activity.status.code`.

## Cardinality Guidance

All three required tags — `Operation`, `LocationId`, and `CustomerResourceId` — must be **low-cardinality and bounded**. Good values: tenant, subscription, environment, region, product tier, work-item type. Bad values: per-request GUIDs, user IDs / emails, timestamps, free-form user input. The same rule applies to any custom attributes you add via `MeasuredOperation.AddAttribute(...)`.

## Disposal

`ServiceLevelIndicator` is a sealed `IDisposable` and is meant to be registered as a singleton — the DI container disposes it (and the `Meter` it created) at host shutdown, so applications never need to call `Dispose()` manually. If you supply your own `Meter` via `ServiceLevelIndicatorOptions.Meter`, you own its lifetime; SLI will not dispose it.

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
