# Copilot Instructions

## Repository Purpose

- This repository provides Service Level Indicator libraries for .NET.
- `Trellis.ServiceLevelIndicators` is the core library for measuring operation latency.
- `Trellis.ServiceLevelIndicators.Asp` adds ASP.NET Core middleware, MVC, and Minimal API integration.
- `Trellis.ServiceLevelIndicators.Asp.ApiVersioning` adds API version as an SLI dimension when used with Asp.Versioning.
- The library emits latency metrics in milliseconds through `System.Diagnostics.Metrics` and OpenTelemetry.

## Metric Contract

- Preserve the emitted instrument name `operation.duration` unless explicitly asked to change it.
- Preserve the tag names `CustomerResourceId` and `LocationId` exactly. Downstream systems depend on these names.
- Preserve the `activity.status.code` and HTTP status behavior unless explicitly asked to change the contract.
- Keep the metric unit in milliseconds.
- Prefer additive changes over breaking changes for metric names, tag names, and public APIs.

## Instrumentation Guidance

- Keep the core library usable outside ASP.NET Core, including console apps, workers, and background jobs.
- Keep ASP.NET Core instrumentation behavior aligned across MVC and Minimal APIs where practical.
- Exception paths in middleware should still emit SLI metrics with failure semantics.
- If `ServiceLevelIndicatorOptions.Meter` supports custom meters, keep OpenTelemetry registration examples aligned with that behavior.

## Cardinality Guidance

- Be careful with dimensions that can explode cardinality.
- Do not introduce samples or defaults that encourage using request IDs, timestamps, email addresses, or arbitrary free text as metric dimensions unless explicitly requested.
- Prefer stable service dimensions such as tenant, subscription, region, environment, product tier, or API version.

## Testing Expectations

- When changing core metric emission or tag behavior, update tests under `Trellis.ServiceLevelIndicators/tests`.
- When changing middleware, endpoint metadata, or HTTP behavior, update tests under `Trellis.ServiceLevelIndicators.Asp/tests`.
- When changing API versioning behavior, update tests under `Trellis.ServiceLevelIndicators.Asp.ApiVersioning/tests`.
- Add regression tests for exception paths, custom meter behavior, and any change to emitted dimensions.
- Prefer focused test updates over broad unrelated refactors.

## Package Constraints

- **FluentAssertions**: Do not upgrade beyond major version 7.x due to a licensing change in version 8+.
- **Swashbuckle.AspNetCore**: Do not upgrade beyond version 6.x due to breaking changes in Microsoft.OpenApi v2.

## Documentation Expectations

- Keep the root README and package READMEs aligned with the actual supported APIs.
- When changing registration or configuration behavior, update code snippets in the relevant README files.
- Keep positioning clear: this library exists to emit SLI-focused latency metrics in milliseconds with service-specific dimensions.

## Change Style

- Prefer small, targeted changes that preserve current public behavior.
- Do not rename public types, extension methods, emitted tags, or documented concepts unless explicitly requested.
- Avoid introducing new dependencies unless they provide clear value and fit the library scope.
