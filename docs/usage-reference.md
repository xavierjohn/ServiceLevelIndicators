# Trellis.ServiceLevelIndicators Usage Reference

This document is a compact reference for choosing the correct package and wiring it into a .NET application.

## Package Matrix

| Scenario | Package | Purpose |
|---|---|---|
| Measure code in any .NET app | `Trellis.ServiceLevelIndicators` | Core latency SLI measurement for console apps, workers, background jobs, and shared libraries |
| Automatically measure ASP.NET Core endpoints | `Trellis.ServiceLevelIndicators.Asp` | Middleware, MVC, and Minimal API integration |
| Add API version as a metric dimension | `Trellis.ServiceLevelIndicators.Asp.ApiVersioning` | Adds `http.api.version` enrichment for apps using Asp.Versioning |

## Metric Contract

These values are part of the library contract and should be treated as stable unless you are intentionally making a breaking change.

| Metric element | Value |
|---|---|
| Meter name | `Trellis.SLI` by default |
| Instrument name | `operation.duration` |
| Unit | milliseconds (`ms`) |
| Required tag | `CustomerResourceId` |
| Required tag | `LocationId` |
| Required tag | `Operation` |
| Required tag | `Outcome` (`Success`, `Failure`, `ClientError`, or `Ignored`) |

For ASP.NET Core, the library also emits `http.request.method` and `http.response.status.code`; `http.api.version` is emitted when API version enrichment is enabled.

## Core Package

Install:

```shell
dotnet add package Trellis.ServiceLevelIndicators
```

Register with OpenTelemetry:

```csharp
builder.Services.AddOpenTelemetry()
    .WithMetrics(metrics =>
    {
        metrics.AddServiceLevelIndicatorInstrumentation();
        metrics.AddOtlpExporter();
    });
```

Register the service:

```csharp
builder.Services.AddServiceLevelIndicator(options =>
{
    options.LocationId = ServiceLevelIndicator.CreateLocationId("public", "westus3");
    options.CustomerResourceId = "tenant-a";
});
```

Measure work:

```csharp
async Task ProcessOrder(ServiceLevelIndicator sli)
{
    using var op = sli.StartMeasuring("ProcessOrder");
    op.AddAttribute("OrderType", "Standard");

    await Task.Delay(50);

    op.SetOutcome(SliOutcome.Success);
}
```

Direct recording is also available when you already know the elapsed time. `Record(...)` emits `CustomerResourceId`, `LocationId`, `Operation`, `Outcome`, and any custom attributes supplied to the call. Manual measurements default to `Ignored` unless you set an outcome.

```csharp
sli.Record("ProcessOrder", elapsedTime: 42);
```

## Custom Meter Registration

If you provide a custom `Meter` in `ServiceLevelIndicatorOptions`, register that same meter with OpenTelemetry.

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
    options.CustomerResourceId = "tenant-a";
});
```

Available registration overloads:

```csharp
metrics.AddServiceLevelIndicatorInstrumentation();
metrics.AddServiceLevelIndicatorInstrumentation("MyCompany.ServiceLevelIndicator");
metrics.AddServiceLevelIndicatorInstrumentation(sliMeter);
```

## ASP.NET Core MVC

Install:

```shell
dotnet add package Trellis.ServiceLevelIndicators.Asp
```

Register services:

```csharp
builder.Services.AddOpenTelemetry()
    .WithMetrics(metrics =>
    {
        metrics.AddServiceLevelIndicatorInstrumentation();
        metrics.AddOtlpExporter();
    });

builder.Services.AddServiceLevelIndicator(options =>
{
    options.LocationId = ServiceLevelIndicator.CreateLocationId("public", "westus3");
    options.CustomerResourceId = "tenant-a";
})
.AddMvc();
```

Add middleware:

```csharp
app.UseServiceLevelIndicator();
```

Common MVC customization points:

```csharp
builder.Services.AddServiceLevelIndicator(options =>
{
    options.LocationId = ServiceLevelIndicator.CreateLocationId("public", "westus3");
})
.AddMvc()
.Enrich(context =>
{
    context.SetCustomerResourceId("tenant-a");
    context.AddAttribute("ProductTier", "Premium");
});
```

Action-level attributes:

```csharp
[HttpGet("orders/{customerId}/{orderType}")]
[ServiceLevelIndicator(Operation = "GetOrder")]
public IActionResult Get(
    [CustomerResourceId] string customerId,
    [Measure(Name = "OrderType")] string orderType)
    => Ok();
```

## ASP.NET Core Minimal APIs

Register services and middleware:

```csharp
builder.Services.AddOpenTelemetry()
    .WithMetrics(metrics =>
    {
        metrics.AddServiceLevelIndicatorInstrumentation();
        metrics.AddOtlpExporter();
    });

builder.Services.AddServiceLevelIndicator(options =>
{
    options.LocationId = ServiceLevelIndicator.CreateLocationId("public", "westus3");
    options.CustomerResourceId = "tenant-a";
    options.AutomaticallyEmitted = false;
});

app.UseServiceLevelIndicator();
```

Mark each endpoint that should emit SLI data:

```csharp
app.MapGet("/orders/{customerId}/{orderType}",
    ([CustomerResourceId] string customerId, [Measure(Name = "OrderType")] string orderType) => Results.Ok())
    .AddServiceLevelIndicator("GetOrder");
```

## API Versioning Package

Install:

```shell
dotnet add package Trellis.ServiceLevelIndicators.Asp.ApiVersioning
```

Register enrichment:

```csharp
builder.Services.AddServiceLevelIndicator(options =>
{
    options.LocationId = ServiceLevelIndicator.CreateLocationId("public", "westus3");
})
.AddMvc()
.AddApiVersion();
```

This adds the `http.api.version` metric dimension when Asp.Versioning is present. The value is the single resolved API version, `Neutral`, `Unspecified`, or an empty string when the requested version is invalid or ambiguous.

## ASP.NET Runtime Helpers

These APIs are useful inside controllers, middleware, and endpoint handlers:

```csharp
var op = HttpContext.GetMeasuredOperation();
op.CustomerResourceId = "tenant-a";
op.AddAttribute("ProductTier", "Premium");

if (HttpContext.TryGetMeasuredOperation(out var measuredOperation))
{
    measuredOperation.AddAttribute("FeatureFlag", "NewCheckout");
}
```

Use `GetMeasuredOperation()` when the route is guaranteed to emit SLI metrics. Use `TryGetMeasuredOperation()` in shared middleware or filters.

## Status Semantics

For non-HTTP code, set the outcome explicitly or use `Measure(...)` / `MeasureAsync(...)` helpers to infer it:

```csharp
op.SetOutcome(SliOutcome.Success);
```

For ASP.NET Core:

| Response outcome | `Outcome` |
|---|---|
| `2xx`, `3xx` | `Success` |
| `400`, `401`, `403`, `404`, `409`, `412`, `422` | `ClientError` |
| `429`, `5xx` | `Failure` |
| Unhandled exceptions | `Failure` |
| Request-aborted cancellations | `Ignored` |

## Cardinality Guidance

Use stable dimensions that support aggregation and alerting.

Good values:

- Tenant or subscription ID
- Region or cloud environment
- Product tier
- API version
- A bounded route category or operation type

Avoid values that can explode cardinality unless your backend is designed for them:

- Email addresses
- Request IDs
- Timestamps
- Arbitrary user input
- Random GUIDs per request

## Common Mistakes

1. Using a custom `Meter` but only registering the default meter name with OpenTelemetry.
2. Treating `CustomerResourceId` as a per-request unique ID instead of a stable service dimension.
3. Forgetting `AddMvc()` when relying on MVC conventions and attribute-based overrides.
4. Forgetting `.AddServiceLevelIndicator()` on Minimal API endpoints when `AutomaticallyEmitted` is `false`.
5. Renaming `CustomerResourceId` or `LocationId` even though downstream systems depend on those exact names.
6. Reusing reserved tag names such as `CustomerResourceId`, `LocationId`, `Operation`, `Outcome`, `activity.status.code`, `http.request.method`, or `http.response.status.code` as custom attributes.

## Public API Cheat Sheet

Core package:

- `AddServiceLevelIndicator(Action<ServiceLevelIndicatorOptions>)`
- `AddServiceLevelIndicatorInstrumentation()`
- `AddServiceLevelIndicatorInstrumentation(string meterName)`
- `AddServiceLevelIndicatorInstrumentation(Meter meter)`
- `ServiceLevelIndicator.StartMeasuring(...)`
- `ServiceLevelIndicator.Record(...)`
- `ServiceLevelIndicator.CreateLocationId(...)`
- `ServiceLevelIndicator.CreateCustomerResourceId(...)`

ASP.NET Core package:

- `UseServiceLevelIndicator()`
- `IServiceLevelIndicatorBuilder.AddMvc()`
- `IServiceLevelIndicatorBuilder.ClassifyHttpOutcome(...)`
- `IServiceLevelIndicatorBuilder.Enrich(...)`
- `IServiceLevelIndicatorBuilder.EnrichAsync(...)`
- `EndpointConventionBuilder.AddServiceLevelIndicator(...)`
- `HttpContext.GetMeasuredOperation()`
- `HttpContext.TryGetMeasuredOperation(...)`

API versioning package:

- `IServiceLevelIndicatorBuilder.AddApiVersion()`
