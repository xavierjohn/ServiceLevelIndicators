using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using SampleWebApplicationSLI;
using ServiceLevelIndicators;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

#region OpenTelemetry
// Build a resource configuration action to set service information.
Action<ResourceBuilder> configureResource = r => r.AddService(
    serviceName: "SampleServiceName",
    serviceVersion: typeof(Program).Assembly.GetName().Version?.ToString() ?? "unknown");

builder.Services.AddOpenTelemetry()
    .ConfigureResource(configureResource)
    .WithMetrics(options =>
{
    options.AddMeter(SampleApiMeters.MeterName);

    options.AddOtlpExporter();
    options.AddConsoleExporter((exporterOptions, metricReaderOptions) =>
        metricReaderOptions.PeriodicExportingMetricReaderOptions.ExportIntervalMilliseconds = 10000
    );
});
builder.Services.AddSingleton<SampleApiMeters>();
builder.Services.AddSingleton((sp) =>
{
    var meters = sp.GetRequiredService<SampleApiMeters>();

    var customerResourceId = ServiceLevelIndicator.CreateCustomerResourceId("SampleServiceName", "SampleAPI");
    var locationId = ServiceLevelIndicator.CreateLocationId("public", "West US 3");
    return new ServiceLevelIndicator(customerResourceId, locationId, meters.Meter);
});
#endregion

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();
app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
