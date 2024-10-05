using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using ServiceLevelIndicators;
using Microsoft.Extensions.Options;
using Swashbuckle.AspNetCore.SwaggerGen;
using Microsoft.Extensions.DependencyInjection.Extensions;
using SampleVersionedWebApplicationSLI;
using Azure.Core;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddTransient<IConfigureOptions<SwaggerGenOptions>, ConfigureSwaggerDefaultOptions>();
builder.Services.AddSwaggerGen(
            options =>
            {
                // add a custom operation filter which sets default values
                options.OperationFilter<AddApiVersionMetadata>();

                var fileName = typeof(Program).Assembly.GetName().Name + ".xml";
                var filePath = Path.Combine(AppContext.BaseDirectory, fileName);

                // integrate XML comments
                options.IncludeXmlComments(filePath);
            });
builder.Services.AddApiVersioning()
        .AddMvc()
        .AddApiExplorer();

builder.Services.AddProblemDetails();

// Build a resource configuration action to set service information.
Action<ResourceBuilder> configureResource = r => r.AddService(
    serviceName: "SampleServiceName",
    serviceVersion: typeof(Program).Assembly.GetName().Version?.ToString() ?? "unknown");
builder.Services.AddOpenTelemetry()
    .ConfigureResource(configureResource)
    .WithMetrics(builder =>
    {
        builder.AddMeter(SampleApiMeters.MeterName);
        builder.AddOtlpExporter();
    });

builder.Services.AddSingleton<SampleApiMeters>();
builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<IConfigureOptions<ServiceLevelIndicatorOptions>, ConfigureServiceLevelIndicatorOptions>());

builder.Services.AddServiceLevelIndicator(options =>
{
    options.LocationId = ServiceLevelIndicator.CreateLocationId("public", AzureLocation.WestUS3.Name);
})
.AddMvc()
.AddApiVersion();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI(
    options =>
    {
        options.RoutePrefix = string.Empty; // make home page the swagger UI
        var descriptions = app.DescribeApiVersions();

        // build a swagger endpoint for each discovered API version
        foreach (var description in descriptions)
        {
            var url = $"/swagger/{description.GroupName}/swagger.json";
            var name = description.GroupName.ToUpperInvariant();
            options.SwaggerEndpoint(url, name);
        }
    });

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
