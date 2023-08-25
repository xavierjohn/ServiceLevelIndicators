﻿namespace ServiceLevelIndicators.Asp.Tests;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Diagnostics.Metrics;
using System.Net;

public class ServiceLevelIndicatorMiddlewareTests
{
    private static readonly Meter s_meter = new("SliTestMeter", "1.0.0");

    [Fact]
    public async Task MiddlewareTest_ReturnsNotFoundForRequest()
    {
        using MeterListener meterListener = new();
        meterListener.InstrumentPublished = (instrument, listener) =>
        {
            if (instrument.Meter.Name is "SliTestMeter")
            {
                listener.EnableMeasurementEvents(instrument);
            }
        };
        meterListener.SetMeasurementEventCallback<int>(OnMeasurementRecorded);
        // Start the meterListener, enabling InstrumentPublished callbacks.
        meterListener.Start();

        using var host = await new HostBuilder()
            .ConfigureWebHost(webBuilder =>
            {
                webBuilder
                    .UseTestServer()
                    .ConfigureServices(services =>
                    {
                        services.AddMvcCore();
                        var partManager = GetApplicationPartManager(services);
                        partManager.FeatureProviders.Add(new ExternalControllersFeatureProvider(typeof(TestController)));
                        services.AddServiceLevelIndicator(options =>
                        {
                            options.Meter = s_meter;
                            options.CustomerResourceId = "SampleCustomerResourceId";
                            options.LocationId = ServiceLevelIndicator.CreateLocationId("public", "West US 3");
                        });
                    })
                    .Configure(app =>
                    {
                        app.UseMiddleware<ServiceLevelIndicatorMiddleware>();
                        app.UseRouting();
                        app.UseEndpoints(endpoints => endpoints.MapControllers());
                    });
            })
            .StartAsync();

        var response = await host.GetTestClient().GetAsync("test");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        static void OnMeasurementRecorded<T>(Instrument instrument, T measurement, ReadOnlySpan<KeyValuePair<string, object?>> tags, object? state)
        {
            Console.WriteLine($"{instrument.Name} recorded measurement {measurement}");
        }
    }

    private static ApplicationPartManager GetApplicationPartManager(IServiceCollection services)
    {
        var partManager = services
            .Last(descriptor => descriptor.ServiceType == typeof(ApplicationPartManager))
            .ImplementationInstance;
        return partManager as ApplicationPartManager ?? throw new InvalidOperationException("Unable to get ApplicationPartManager");
    }

    [ApiController]
    public class TestController : ControllerBase
    {
        [HttpGet]
        public static string Get()
        {
            return "Hello";
        }
    }
}
