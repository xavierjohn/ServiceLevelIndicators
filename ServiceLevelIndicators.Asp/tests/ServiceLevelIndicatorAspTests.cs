namespace ServiceLevelIndicators.Asp.Tests;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Diagnostics.Metrics;
using System.Net;

public partial class ServiceLevelIndicatorAspTests
{
    private bool _callbackCalled;

    [Fact]
    public async Task Default_SLI_Metrics_is_emitted()
    {
        const string meterName = "DefaultSliTestMeter";
        using Meter meter = new(meterName, "1.0.0");
        using MeterListener meterListener = new();
        meterListener.InstrumentPublished = (instrument, listener) =>
        {
            if (instrument.Meter.Name is meterName)
                listener.EnableMeasurementEvents(instrument);
        };
        meterListener.SetMeasurementEventCallback<long>(OnMeasurementRecorded);
        meterListener.Start();

        using var host = await CreateAndStartHost(meter);

        var response = await host.GetTestClient().GetAsync("test");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        void OnMeasurementRecorded(Instrument instrument, long measurement, ReadOnlySpan<KeyValuePair<string, object?>> tags, object? state)
        {
            _callbackCalled = true;
            var expectedTags = new KeyValuePair<string, object?>[]
            {
                new KeyValuePair<string, object?>("CustomerResourceId", "TestCustomerResourceId"),
                new KeyValuePair<string, object?>("LocationId", "ms-loc://az/public/West US 3"),
                new KeyValuePair<string, object?>("Operation", "GET Test"),
                new KeyValuePair<string, object?>("Status", "Ok"),
                new KeyValuePair<string, object?>("HttpStatusCode", 200),
            };

            instrument.Name.Should().Be("LatencySLI");
            instrument.Unit.Should().Be("ms");
            measurement.Should().BeGreaterOrEqualTo(1);

            tags.ToArray().Should().BeEquivalentTo(expectedTags);
        }

        _callbackCalled.Should().BeTrue();
    }

    [Fact]
    public async Task Operation_name_will_be_set()
    {
        const string meterName = "OperationSliTestMeter";
        using Meter meter = new(meterName, "1.0.0");
        using MeterListener meterListener = new();
        meterListener.InstrumentPublished = (instrument, listener) =>
        {
            if (instrument.Meter.Name is meterName)
                listener.EnableMeasurementEvents(instrument);
        };
        meterListener.SetMeasurementEventCallback<long>(OnMeasurementRecorded);
        meterListener.Start();

        using var host = await CreateAndStartHost(meter);

        var response = await host.GetTestClient().GetAsync("test/operation");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        void OnMeasurementRecorded(Instrument instrument, long measurement, ReadOnlySpan<KeyValuePair<string, object?>> tags, object? state)
        {
            _callbackCalled = true;
            var expectedTags = new KeyValuePair<string, object?>[]
            {
                new KeyValuePair<string, object?>("CustomerResourceId", "TestCustomerResourceId"),
                new KeyValuePair<string, object?>("LocationId", "ms-loc://az/public/West US 3"),
                new KeyValuePair<string, object?>("Operation", "TestOperation"),
                new KeyValuePair<string, object?>("Status", "Ok"),
                new KeyValuePair<string, object?>("HttpStatusCode", 200),
            };

            instrument.Name.Should().Be("LatencySLI");
            instrument.Unit.Should().Be("ms");
            measurement.Should().BeGreaterOrEqualTo(1);

            tags.ToArray().Should().BeEquivalentTo(expectedTags);
        }

        _callbackCalled.Should().BeTrue();
    }

    private static async Task<IHost> CreateAndStartHost(Meter meter) =>
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
                        });
                    })
                    .Configure(app =>
                    {
                        app.UseRouting()
                           .UseServiceLevelIndicator()
                           .UseEndpoints(endpoints => endpoints.MapControllers());
                    });
            })
            .StartAsync();
}
