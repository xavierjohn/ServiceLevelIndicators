using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using SampleWebApplicationSLI;
using ServiceLevelIndicators;
using Microsoft.Extensions.Options;
using Swashbuckle.AspNetCore.SwaggerGen;
using Microsoft.Extensions.DependencyInjection.Extensions;
using SampleVersionedWebApplicationSLI;

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

                // integrate xml comments
                options.IncludeXmlComments(filePath);
            });
builder.Services.AddApiVersioning()
        .AddMvc()
        .AddApiExplorer();

builder.Services.AddProblemDetails();
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
builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<IConfigureOptions<ServiceLevelIndicatorOptions>, ConfigureServiceLevelIndicatorOptions>());

builder.Services.AddServiceLevelIndicator(options =>
{
    Guid serviceTree = Guid.NewGuid();
    options.CustomerResourceId = ServiceLevelIndicator.CreateCustomerResourceId(serviceTree);
    options.LocationId = ServiceLevelIndicator.CreateLocationId("public", "westus2");
});

#endregion

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
app.UseHttpsRedirection();
app.UseServiceLevelIndicator();
app.UseAuthorization();

app.MapControllers();

app.Run();
