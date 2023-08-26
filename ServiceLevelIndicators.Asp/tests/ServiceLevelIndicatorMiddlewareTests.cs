namespace ServiceLevelIndicators.Asp.Tests;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Diagnostics.Metrics;
using System.Net;

public partial class ServiceLevelIndicatorMiddlewareTests
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
                        services.AddControllers();
                        services.AddServiceLevelIndicator(options =>
                        {
                            options.Meter = s_meter;
                            options.CustomerResourceId = "SampleCustomerResourceId";
                            options.LocationId = ServiceLevelIndicator.CreateLocationId("public", "West US 3");
                        });
                    })
                    .Configure(app =>
                    {
                        app.UseRouting();
                        app.UseMiddleware<ServiceLevelIndicatorMiddleware>();
                        app.UseEndpoints(endpoints => endpoints.MapControllers());
                    });
            })
            .StartAsync();

        var response = await host.GetTestClient().GetAsync("test");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var str = await response.Content.ReadAsStringAsync();

        static void OnMeasurementRecorded<T>(Instrument instrument, T measurement, ReadOnlySpan<KeyValuePair<string, object?>> tags, object? state)
        {
            Console.WriteLine($"{instrument.Name} recorded measurement {measurement}");
        }
    }
}
