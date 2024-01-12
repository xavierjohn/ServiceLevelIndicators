namespace ServiceLevelIndicators.Asp.Tests;
using System;
using Microsoft.Extensions.DependencyInjection;

internal static class IServiceCollectionExtensions
{
    public static IServiceLevelIndicatorBuilder AddTestEnrichment(this IServiceLevelIndicatorBuilder builder, string key, string value)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.Services.AddSingleton<IMeasuredOperationEnrichment>(new TestMeasuredOperationLatencyEnrich(key, value));
        return builder;
    }
}
