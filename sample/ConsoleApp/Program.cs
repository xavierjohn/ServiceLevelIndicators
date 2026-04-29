using System.Reflection;
using Azure.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OpenTelemetry;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using Trellis.ServiceLevelIndicators;

#pragma warning disable CA1848 // Use the LoggerMessage delegates

const string ProgramName = "SampleConsoleSLI";
string version = Assembly
    .GetExecutingAssembly()
    .GetCustomAttribute<AssemblyFileVersionAttribute>()!
    .Version;
ResourceBuilder resourceBuilder = ResourceBuilder
    .CreateEmpty()
    .AddService(ProgramName, serviceVersion: version, serviceInstanceId: Environment.MachineName);

ServiceCollection services = new();
services.AddServiceLevelIndicator(r =>
{
    r.CustomerResourceId = ProgramName; // Stable resource this background workload is measuring.
    r.LocationId = ServiceLevelIndicator.CreateLocationId("public", AzureLocation.WestUS3.Name);
});

services
    .AddLogging(builder => builder.AddOpenTelemetry(options =>
        {
            options.SetResourceBuilder(resourceBuilder);
            options.AddConsoleExporter();
        }));

var serviceProvider = services.BuildServiceProvider();

var logger = serviceProvider.GetRequiredService<ILogger<Program>>();

using MeterProvider meterProvider = Sdk.CreateMeterProviderBuilder()
                .SetResourceBuilder(resourceBuilder)
                .AddConsoleExporter()
                .AddServiceLevelIndicatorInstrumentation()
                .Build();

var serviceLevelIndicator = serviceProvider.GetRequiredService<ServiceLevelIndicator>();

try
{
    await serviceLevelIndicator.MeasureAsync("OperationWork", async () =>
    {
        logger.LogInformation("Starting to do some work...");
        await Task.Delay(1000); // Simulate some work
        logger.LogInformation("Work done.");
    });
}
catch (Exception ex)
{
    logger.LogError(ex, "An error occurred doing work.");
}
