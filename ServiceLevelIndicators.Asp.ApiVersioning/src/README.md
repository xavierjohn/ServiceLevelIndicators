# ServiceLevelIndicators.Asp.ApiVersioning

[![NuGet Package](https://img.shields.io/nuget/v/ServiceLevelIndicators.Asp.ApiVersioning.svg)](https://www.nuget.org/packages/ServiceLevelIndicators.Asp.ApiVersioning)

Adds **API version** as a metric dimension to [ServiceLevelIndicators.Asp](https://www.nuget.org/packages/ServiceLevelIndicators.Asp) SLI metrics. Works with the [Asp.Versioning](https://github.com/dotnet/aspnet-api-versioning) package.

## Installation

```shell
dotnet add package ServiceLevelIndicators.Asp.ApiVersioning
```

> Requires [ServiceLevelIndicators.Asp](https://www.nuget.org/packages/ServiceLevelIndicators.Asp) (installed automatically as a dependency).

## Usage

Chain `AddApiVersion()` onto the SLI builder:

```csharp
builder.Services.AddServiceLevelIndicator(options =>
{
    options.LocationId = ServiceLevelIndicator.CreateLocationId("public", "westus3");
})
.AddMvc()
.AddApiVersion();
```

This registers `ApiVersionEnrichment`, which reads the resolved API version from the request and emits it as an attribute on every SLI measurement.

## Emitted Attribute

| Attribute | Description |
|-----------|-------------|
| `http.api.version` | The resolved API version string (e.g. `1.0`, `2024-01-15`), `Neutral`, or `Unspecified` |

This attribute is added alongside all the standard attributes emitted by `ServiceLevelIndicators.Asp` (`Operation`, `CustomerResourceId`, `LocationId`, `activity.status.code`, `http.response.status.code`).

## Further Reading

- [Full documentation and samples](https://github.com/xavierjohn/ServiceLevelIndicators)
- [ASP.NET API Versioning](https://github.com/dotnet/aspnet-api-versioning)
