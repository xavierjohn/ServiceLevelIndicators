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
    const string MeterName = "SliTestMeter";
    private bool _callbackCalled;

    [Fact]
    public async Task Default_SLI_Metrics_is_emitted()
    {
        using Meter meter = new(MeterName, "1.0.0");
        using MeterListener meterListener = new();
        meterListener.InstrumentPublished = (instrument, listener) =>
        {
            if (instrument.Meter.Name is MeterName)
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
    public async Task Override_Operation_name()
    {
        using Meter meter = new(MeterName, "1.0.0");
        using MeterListener meterListener = new();
        meterListener.InstrumentPublished = (instrument, listener) =>
        {
            if (instrument.Meter.Name is MeterName)
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

    [Fact]
    public async Task Override_CustomerResourceId()
    {
        using Meter meter = new(MeterName, "1.0.0");
        using MeterListener meterListener = new();
        meterListener.InstrumentPublished = (instrument, listener) =>
        {
            if (instrument.Meter.Name is MeterName)
                listener.EnableMeasurementEvents(instrument);
        };
        meterListener.SetMeasurementEventCallback<long>(OnMeasurementRecorded);
        meterListener.Start();

        using var host = await CreateAndStartHost(meter);

        var response = await host.GetTestClient().GetAsync("test/customer_resourceid/myId");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        void OnMeasurementRecorded(Instrument instrument, long measurement, ReadOnlySpan<KeyValuePair<string, object?>> tags, object? state)
        {
            _callbackCalled = true;
            var expectedTags = new KeyValuePair<string, object?>[]
            {
                new KeyValuePair<string, object?>("CustomerResourceId", "myId"),
                new KeyValuePair<string, object?>("LocationId", "ms-loc://az/public/West US 3"),
                new KeyValuePair<string, object?>("Operation", "GET Test/customer_resourceid/{id}"),
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
    public async Task Add_custom_SLI_attribute()
    {
        using Meter meter = new(MeterName, "1.0.0");
        using MeterListener meterListener = new();
        meterListener.InstrumentPublished = (instrument, listener) =>
        {
            if (instrument.Meter.Name is MeterName)
                listener.EnableMeasurementEvents(instrument);
        };
        meterListener.SetMeasurementEventCallback<long>(OnMeasurementRecorded);
        meterListener.Start();

        using var host = await CreateAndStartHost(meter);

        var response = await host.GetTestClient().GetAsync("test/custom_attribute/mickey");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        void OnMeasurementRecorded(Instrument instrument, long measurement, ReadOnlySpan<KeyValuePair<string, object?>> tags, object? state)
        {
            _callbackCalled = true;
            var expectedTags = new KeyValuePair<string, object?>[]
            {
                new KeyValuePair<string, object?>("CustomerResourceId", "TestCustomerResourceId"),
                new KeyValuePair<string, object?>("LocationId", "ms-loc://az/public/West US 3"),
                new KeyValuePair<string, object?>("Operation", "GET Test/custom_attribute/{value}"),
                new KeyValuePair<string, object?>("Status", "Ok"),
                new KeyValuePair<string, object?>("HttpStatusCode", 200),
                new KeyValuePair<string, object?>("CustomAttribute", "mickey"),
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
