﻿namespace SampleWebApplicationSLI;

using Microsoft.Extensions.Options;
using ServiceLevelIndicators;

internal sealed class ConfigureServiceLevelIndicatorOptions : IConfigureOptions<ServiceLevelIndicatorOptions>
{
    private readonly SampleApiMeters meters;

    public ConfigureServiceLevelIndicatorOptions(SampleApiMeters meters) => this.meters = meters;

    public void Configure(ServiceLevelIndicatorOptions options) => options.Meter = meters.Meter;
}
