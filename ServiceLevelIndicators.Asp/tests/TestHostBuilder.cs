namespace ServiceLevelIndicators.Asp.Tests;
using System.Diagnostics.Metrics;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

internal class TestHostBuilder
{
    internal const int MillisecondsDelay = 200;

    public static async Task<IHost> CreateHostWithSli(Meter meter) =>
        await new HostBuilder()
            .ConfigureWebHost(webBuilder =>
            {
                webBuilder
                    .UseTestServer()
                    .ConfigureServices(services =>
                    {
                        services.AddControllers();
                        services.AddServiceLevelIndicator(options =>
                        {
                            options.Meter = meter;
                            options.CustomerResourceId = "TestCustomerResourceId";
                            options.LocationId = ServiceLevelIndicator.CreateLocationId("public", "West US 3");
                        }).AddMvc();
                    })
                    .Configure(app =>
                    {
                        app.UseRouting()
                           .UseServiceLevelIndicator()
                           .Use(async (context, next) =>
                           {
                               await Task.Delay(MillisecondsDelay);
                               await next(context);
                           })
                           .UseEndpoints(endpoints => endpoints.MapControllers());
                    });
            })
            .StartAsync();

    public static async Task<IHost> CreateHostWithSliEnriched(Meter meter) =>
    await new HostBuilder()
        .ConfigureWebHost(webBuilder =>
        {
            webBuilder
                .UseTestServer()
                .ConfigureServices(services =>
                {
                    services.AddControllers();
                    services.AddServiceLevelIndicator(options =>
                    {
                        options.Meter = meter;
                        options.CustomerResourceId = "TestCustomerResourceId";
                        options.LocationId = ServiceLevelIndicator.CreateLocationId("public", "West US 3");
                    })
                    .AddMvc()
                    .AddTestEnrichment("foo", "bar")
                    .AddTestEnrichment("test", "again");
                })
                .Configure(app =>
                {
                    app.UseRouting()
                       .UseServiceLevelIndicator()
                       .Use(async (context, next) =>
                       {
                           await Task.Delay(MillisecondsDelay);
                           await next(context);
                       })
                       .UseEndpoints(endpoints => endpoints.MapControllers());
                });
        })
        .StartAsync();

    public static async Task<IHost> CreateHostWithoutAutomaticSli(Meter meter)
    {
        return await new HostBuilder()
            .ConfigureWebHost(webBuilder =>
            {
                webBuilder
                    .UseTestServer()
                    .ConfigureServices(services =>
                    {
                        services.AddControllers();
                        services.AddServiceLevelIndicator(options =>
                        {
                            options.Meter = meter;
                            options.CustomerResourceId = "TestCustomerResourceId";
                            options.LocationId = ServiceLevelIndicator.CreateLocationId("public", "West US 3");
                            options.AutomaticallyEmitted = false;
                        }).AddMvc();
                    })
                    .Configure(app =>
                    {
                        app.UseRouting()
                           .UseServiceLevelIndicator()
                           .Use(async (context, next) =>
                           {
                               await Task.Delay(MillisecondsDelay);
                               await next(context);
                           })
                           .UseEndpoints(endpoints => endpoints.MapControllers());
                    });
            })
            .StartAsync();
    }

    public static async Task<IHost> CreateHostWithoutSli()
    {
        return await new HostBuilder()
            .ConfigureWebHost(webBuilder =>
            {
                webBuilder
                    .UseTestServer()
                    .ConfigureServices(services =>
                    {
                        services.AddControllers();
                    })
                    .Configure(app =>
                    {
                        app.UseRouting()
                           .Use(async (context, next) =>
                           {
                               await Task.Delay(MillisecondsDelay);
                               await next(context);
                           })
                           .UseEndpoints(endpoints => endpoints.MapControllers());
                    });
            })
            .StartAsync();
    }
}

