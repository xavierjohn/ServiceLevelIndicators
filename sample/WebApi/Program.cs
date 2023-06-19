using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using SampleWebApplicationSLI;
using Asp.ServiceLevelIndicators;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    var filePath = Path.Combine(AppContext.BaseDirectory, "Asp.SampleWebApplicationSLI.xml");
    options.IncludeXmlComments(filePath);
});

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
    });

builder.Services.AddSingleton<SampleApiMeters>();
builder.Services.AddSingleton<IServiceLevelIndicatorMeter>(sp =>
{
    var meters = sp.GetRequiredService<SampleApiMeters>();
    return new ServiceLevelIndicatorMeter(meters.Meter);
});
builder.Services.AddServiceLevelIndicator(options =>
{
    options.CustomerResourceId = "SampleCustomerResourceId";
    options.LocationId = ServiceLevelIndicator.CreateLocationId("public", "West US 3");
});

#endregion

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();
app.UseHttpsRedirection();
app.UseServiceLevelIndicator();
app.UseAuthorization();

app.MapControllers();

app.Run();
