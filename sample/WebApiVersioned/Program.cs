using Azure.Core;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using Scalar.AspNetCore;
using Trellis.ServiceLevelIndicators;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddApiVersioning()
        .AddMvc()
        .AddApiExplorer();

builder.Services.AddProblemDetails();

Action<ResourceBuilder> configureResource = r => r.AddService(
    serviceName: "SampleVersionedWebApplicationSLI",
    serviceVersion: typeof(Program).Assembly.GetName().Version?.ToString() ?? "unknown");
builder.Services.AddOpenTelemetry()
    .ConfigureResource(configureResource)
    .WithMetrics(builder =>
    {
        builder.AddServiceLevelIndicatorInstrumentation();
        builder.AddOtlpExporter();
    });

builder.Services.AddServiceLevelIndicator(options => options.LocationId = ServiceLevelIndicator.CreateLocationId("public", AzureLocation.WestUS3.Name))
.AddMvc()
.AddApiVersion();

var app = builder.Build();

// TODO: Use .AddOpenApi() from Asp.Versioning.OpenApi with WithDocumentPerVersion()
// and AddScalarTransformers() once a stable release is available.
app.MapOpenApi();
app.MapScalarApiReference();

// Random delay.
Random rnd = new Random();
app.Use(async (context, next) =>
{
    await Task.Delay(rnd.Next(40, 200));
    await next(context);
});
app.UseHttpsRedirection();
app.UseServiceLevelIndicator();
app.UseAuthorization();

app.MapControllers();

app.Run();
