using Azure.Core;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using Scalar.AspNetCore;
using SampleMinimalApiSli;
using Trellis.ServiceLevelIndicators;

var builder = WebApplication.CreateBuilder(args);

// Configure the HTTP request pipeline.

var summaries = new[]
{
    "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
};

builder.Services.AddOpenApi();

// Build a resource configuration action to set service information.

Action<ResourceBuilder> configureResource = r => r.AddService(
    serviceName: "SampleMinimalApiSli",
    serviceVersion: typeof(Program).Assembly.GetName().Version?.ToString() ?? "unknown");

builder.Services.AddOpenTelemetry()
    .ConfigureResource(configureResource)
    .WithMetrics(builder =>
    {
        builder.AddServiceLevelIndicatorInstrumentation();
        builder.AddOtlpExporter();
    });

builder.Services.AddServiceLevelIndicator(options =>
{
    options.CustomerResourceId = "SampleCustomerResourceId";
    options.LocationId = ServiceLevelIndicator.CreateLocationId("public", AzureLocation.WestUS3.Name);
})
.AddHttpMethod();

// Add services to the container.

var app = builder.Build();

app.MapGet(
        "/weatherforecast/{cid}",
        ([CustomerResourceId] string cid) =>
            Enumerable.Range(1, 5).Select(index =>
            new WeatherForecast
            (
                DateTime.Now.AddDays(index),
                Random.Shared.Next(-20, 55),
                summaries[Random.Shared.Next(summaries.Length)]
            )))
   .AddServiceLevelIndicator();

app.MapGet(
        "/background/{workType:regex(^(Quick|Standard|Slow)$)}",
        ([Measure("WorkType")] string workType) => Task.Delay(workType switch
        {
            nameof(WorkType.Quick) => TimeSpan.FromMilliseconds(250),
            nameof(WorkType.Standard) => TimeSpan.FromSeconds(1),
            nameof(WorkType.Slow) => TimeSpan.FromSeconds(2),
            _ => TimeSpan.FromSeconds(1)
        }))
   .AddServiceLevelIndicator("background_work");

app.UseUserRoute();
app.MapOpenApi();
app.MapScalarApiReference();
app.UseHttpsRedirection();
app.UseServiceLevelIndicator();
app.Run();

internal record WeatherForecast(DateTime Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}

internal enum WorkType
{
    Quick,
    Standard,
    Slow
}
