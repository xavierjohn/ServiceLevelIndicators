# ServiceLevelIndicators.Asp

[![NuGet Package](https://img.shields.io/nuget/v/ServiceLevelIndicators.Asp.svg)](https://www.nuget.org/packages/ServiceLevelIndicators.Asp)

ASP.NET Core middleware that **automatically emits Service Level Indicator (SLI) latency metrics in milliseconds** for every API operation. Built on the [ServiceLevelIndicators](https://www.nuget.org/packages/ServiceLevelIndicators) core library, it helps teams move from generic request timing to service-specific latency metrics with dimensions such as operation, customer, location, and status.

For API versioning support, add [ServiceLevelIndicators.Asp.ApiVersioning](https://www.nuget.org/packages/ServiceLevelIndicators.Asp.ApiVersioning).

## When To Use This Package

Choose `ServiceLevelIndicators.Asp` when you are building an ASP.NET Core application and want SLI metrics emitted automatically for MVC controllers or Minimal API endpoints.

Use [ServiceLevelIndicators](https://www.nuget.org/packages/ServiceLevelIndicators) if you only need manual measurement in non-HTTP code. Add [ServiceLevelIndicators.Asp.ApiVersioning](https://www.nuget.org/packages/ServiceLevelIndicators.Asp.ApiVersioning) when your ASP.NET Core app uses Asp.Versioning and you want the resolved API version emitted as a metric dimension.

## Installation

```shell
dotnet add package ServiceLevelIndicators.Asp
```

## Quick Start — MVC Controllers

```csharp
// 1. Register with OpenTelemetry
builder.Services.AddOpenTelemetry()
    .WithMetrics(metrics =>
    {
        metrics.AddServiceLevelIndicatorInstrumentation();
        metrics.AddOtlpExporter();
    });

// 2. Configure SLI — AddMvc() enables attribute-based overrides
builder.Services.AddServiceLevelIndicator(options =>
{
    options.LocationId = ServiceLevelIndicator.CreateLocationId("public", "westus3");
})
.AddMvc();

// 3. Add the middleware
app.UseServiceLevelIndicator();
```

## Quick Start — Minimal APIs

```csharp
builder.Services.AddServiceLevelIndicator(options =>
{
    options.LocationId = ServiceLevelIndicator.CreateLocationId("public", "westus3");
});

app.UseServiceLevelIndicator();

// Mark individual endpoints
app.MapGet("/hello", () => "Hello World!")
    .AddServiceLevelIndicator();
```

If you configure a custom `Meter` in `ServiceLevelIndicatorOptions`, register that same meter with OpenTelemetry by calling `AddServiceLevelIndicatorInstrumentation(meter)`.

## Emitted Metrics

A meter named `ServiceLevelIndicator` with instrument `operation.duration` (milliseconds) is emitted with the following attributes:

| Attribute | Description |
|-----------|-------------|
| `Operation` | Defaults to the route template, e.g. `GET WeatherForecast` |
| `CustomerResourceId` | Identifies the customer or caller |
| `LocationId` | Where the service is running |
| `activity.status.code` | `Ok` (2xx), `Error` (5xx), or `Unset` (other) |
| `http.response.status.code` | The HTTP response status code |
| `http.request.method` | *(Optional)* The HTTP method — enabled via `AddHttpMethod()` |

## Customizations

### Add HTTP method as a dimension

```csharp
builder.Services.AddServiceLevelIndicator(options => { /* ... */ })
    .AddMvc()
    .AddHttpMethod();
```

### Enrich with custom data

Use `Enrich` (or `EnrichAsync`) to set `CustomerResourceId` or add custom attributes:

```csharp
builder.Services.AddServiceLevelIndicator(options => { /* ... */ })
    .AddMvc()
    .Enrich(context =>
    {
        var upn = context.HttpContext.User.Claims
            .FirstOrDefault(c => c.Type == "upn")?.Value ?? "Unknown";
        context.SetCustomerResourceId(upn);
        context.AddAttribute("UserPrincipalName", upn);
    });
```

### Override the operation name

```csharp
[HttpGet("MyAction")]
[ServiceLevelIndicator(Operation = "MyCustomName")]
public IActionResult Get() => Ok();
```

### Set CustomerResourceId from a route parameter

```csharp
[HttpGet("get-by-zip-code/{zipCode}")]
public IActionResult GetByZipcode([CustomerResourceId] string zipCode) => Ok();
```

Or imperatively:

```csharp
HttpContext.GetMeasuredOperation().CustomerResourceId = customerResourceId;
```

### Add custom attributes from route parameters

Parameters decorated with `[Measure]` are automatically added as metric dimensions:

```csharp
[HttpGet("name/{first}/{surname}")]
public IActionResult Get([Measure] string first, [CustomerResourceId] string surname) => Ok();
```

### Add custom attributes manually

```csharp
HttpContext.GetMeasuredOperation().AddAttribute("CustomKey", value);

// Safe version for middleware (won't throw if SLI is not configured for the route)
if (HttpContext.TryGetMeasuredOperation(out var op))
    op.AddAttribute("CustomKey", value);
```

## Cardinality Guidance

Use `CustomerResourceId`, `[Measure]`, and custom attributes for stable service dimensions such as tenant, subscription, region, or API flavor. Avoid unbounded values like request IDs, email addresses, timestamps, or arbitrary user input unless you explicitly want high-cardinality metrics and your backend can absorb the cost.

### Opt-in mode

To disable automatic SLI emission on all controllers:

```csharp
options.AutomaticallyEmitted = false;
```

Then add `[ServiceLevelIndicator]` only to the controllers you want measured.

## Further Reading

- [Full documentation and samples](https://github.com/xavierjohn/ServiceLevelIndicators)
- [Package selection and usage reference](https://github.com/xavierjohn/ServiceLevelIndicators/blob/main/docs/usage-reference.md)
