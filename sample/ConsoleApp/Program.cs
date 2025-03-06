using System.Diagnostics;
using System.Reflection;
using Azure.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OpenTelemetry;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using ServiceLevelIndicators;

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
services.Configure<ServiceLevelIndicatorOptions>(r =>
{
    r.CustomerResourceId = ProgramName; // Customer ID if this work is done on behalf of a customer.
    r.LocationId = ServiceLevelIndicator.CreateLocationId("public", AzureLocation.WestUS3.Name);
});

services
    .AddLogging(builder =>
    {
        builder.AddOpenTelemetry(options =>
        {
            options.SetResourceBuilder(resourceBuilder);
            options.AddConsoleExporter();
        });
    })
    .AddSingleton<ServiceLevelIndicator>(); ;

var serviceProvider = services.BuildServiceProvider();

var logger = serviceProvider.GetRequiredService<ILogger<Program>>();

using MeterProvider meterProvider = Sdk.CreateMeterProviderBuilder()
                .SetResourceBuilder(resourceBuilder)
                .AddConsoleExporter()
                .AddServiceLevelIndicatorInstrumentation()
                .Build();

var serviceLevelIndicator = serviceProvider.GetRequiredService<ServiceLevelIndicator>();
using MeasuredOperation measuredOperation = serviceLevelIndicator.StartMeasuring("OperationWork");

try
{
    logger.LogInformation("Starting to do some work...");
    await Task.Delay(1000); // Simulate some work
    logger.LogInformation("Work done.");
    measuredOperation.SetActivityStatusCode(ActivityStatusCode.Ok);
}
catch (Exception ex)
{
    measuredOperation.SetActivityStatusCode(ActivityStatusCode.Error);
    logger.LogError(ex, "An error occurred doing work.");
}
