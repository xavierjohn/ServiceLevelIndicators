using Azure.Core;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using ServiceLevelIndicators;

var builder = WebApplication.CreateBuilder(args);

// Configure the HTTP request pipeline.

var summaries = new[]
{
    "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
};

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{

    var fileName = typeof(Program).Assembly.GetName().Name + ".xml";
    var filePath = Path.Combine(AppContext.BaseDirectory, fileName);
    options.IncludeXmlComments(filePath);
});

// Build a resource configuration action to set service information.

Action<ResourceBuilder> configureResource = r => r.AddService(
    serviceName: "SampleServiceName",
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
        "/background/{wait_seconds}",
        ([Measure] int wait_seconds) => Task.Delay(TimeSpan.FromSeconds(wait_seconds)))
   .AddServiceLevelIndicator("background_work");


app.UseSwagger();
app.UseSwaggerUI();
app.UseHttpsRedirection();
app.UseServiceLevelIndicator();
app.Run();

internal record WeatherForecast(DateTime Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}
