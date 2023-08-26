namespace ServiceLevelIndicators.Asp.Tests;

using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

public class Startup
{
    public Startup(IConfiguration configuration)
    {
        Configuration = configuration;
    }

    public IConfiguration Configuration { get; }

    public static void ConfigureServices(IServiceCollection services)
    {
        services.AddControllers();
        services.AddMvcCore();
        services.AddHostedService<ApplicationPartsLogger>();
    }

    public static void Configure(IApplicationBuilder app)
    {
        app.UseRouting();

        app.UseEndpoints(endpoints =>
        {
            endpoints.MapControllers();
        });
    }
}
