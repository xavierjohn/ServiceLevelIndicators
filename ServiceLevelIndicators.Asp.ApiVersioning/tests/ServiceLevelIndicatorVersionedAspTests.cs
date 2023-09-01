namespace ServiceLevelIndicators.Asp.ApiVersioning.Tests;

using global::Asp.Versioning;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Diagnostics.Metrics;
using System.Globalization;
using System.Net;
using Xunit.Abstractions;

public class ServiceLevelIndicatorVersionedAspTests : IDisposable
{
    private readonly Meter _meter;
    private readonly MeterListener _meterListener;
    private readonly ITestOutputHelper _output;
    private bool _callbackCalled;
    private bool _disposedValue;

    public ServiceLevelIndicatorVersionedAspTests(ITestOutputHelper output)
    {
        _output = output;
        const string MeterName = "SliTestMeter";
        _meter = new(MeterName, "1.0.0");
        _meterListener = new()
        {
            InstrumentPublished = (instrument, listener) =>
            {
                if (instrument.Meter.Name is MeterName)
                    listener.EnableMeasurementEvents(instrument);
            }
        };
    }

    [Fact]
    public async Task SLI_Metrics_is_emitted_with_API_version_as_query_parameter()
    {
        _meterListener.SetMeasurementEventCallback<long>(OnMeasurementRecorded);
        _meterListener.Start();

        using var host = await CreateHost(_meter);

        var response = await host.GetTestClient().GetAsync("testSingle?api-version=2023-08-29");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        void OnMeasurementRecorded(Instrument instrument, long measurement, ReadOnlySpan<KeyValuePair<string, object?>> tags, object? state)
        {
            var expectedTags = new KeyValuePair<string, object?>[]
            {
                new KeyValuePair<string, object?>("CustomerResourceId", "TestCustomerResourceId"),
                new KeyValuePair<string, object?>("LocationId", "ms-loc://az/public/West US 3"),
                new KeyValuePair<string, object?>("Operation", "GET TestSingle"),
                new KeyValuePair<string, object?>("Status", "Ok"),
                new KeyValuePair<string, object?>("HttpStatusCode", 200),
                new KeyValuePair<string, object?>("api_version", "2023-08-29"),
            };

            ValidateMetrics(instrument, measurement, tags, expectedTags);
        }

        _callbackCalled.Should().BeTrue();
    }

    [Fact]
    public async Task SLI_Metrics_is_emitted_with_API_version_as_header()
    {
        _meterListener.SetMeasurementEventCallback<long>(OnMeasurementRecorded);
        _meterListener.Start();

        using var host = await CreateHost(_meter);

        var httpClient = host.GetTestClient();
        httpClient.DefaultRequestHeaders.Add("api-version", "2023-08-29");
        var response = await httpClient.GetAsync("testSingle");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        void OnMeasurementRecorded(Instrument instrument, long measurement, ReadOnlySpan<KeyValuePair<string, object?>> tags, object? state)
        {
            var expectedTags = new KeyValuePair<string, object?>[]
            {
                new KeyValuePair<string, object?>("CustomerResourceId", "TestCustomerResourceId"),
                new KeyValuePair<string, object?>("LocationId", "ms-loc://az/public/West US 3"),
                new KeyValuePair<string, object?>("Operation", "GET TestSingle"),
                new KeyValuePair<string, object?>("Status", "Ok"),
                new KeyValuePair<string, object?>("HttpStatusCode", 200),
                new KeyValuePair<string, object?>("api_version", "2023-08-29"),
            };

            ValidateMetrics(instrument, measurement, tags, expectedTags);
        }

        _callbackCalled.Should().BeTrue();
    }

    [Fact]
    public async Task SLI_Metrics_is_emitted_with_multiple_API_versions_on_Request()
    {
        _meterListener.SetMeasurementEventCallback<long>(OnMeasurementRecorded);
        _meterListener.Start();

        using var host = await CreateHost(_meter);

        var response = await host.GetTestClient().GetAsync("testSingle?api-version=2023-08-29&api-version=2023-09-01");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        void OnMeasurementRecorded(Instrument instrument, long measurement, ReadOnlySpan<KeyValuePair<string, object?>> tags, object? state)
        {
            var expectedTags = new KeyValuePair<string, object?>[]
            {
                new KeyValuePair<string, object?>("api_version", "2023-08-29"),
                new KeyValuePair<string, object?>("CustomerResourceId", "TestCustomerResourceId"),
                new KeyValuePair<string, object?>("LocationId", "ms-loc://az/public/West US 3"),
                new KeyValuePair<string, object?>("Operation", "GET TestSingle"),
                new KeyValuePair<string, object?>("Status", "Ok"),
                new KeyValuePair<string, object?>("HttpStatusCode", 200),
            };

            ValidateMetrics(instrument, measurement, tags, expectedTags);
        }

        _callbackCalled.Should().BeTrue();
    }

    [Fact]
    public async Task SLI_Metrics_is_emitted_with_multiple_API_versions_on_Request_and_Controller()
    {
        _meterListener.SetMeasurementEventCallback<long>(OnMeasurementRecorded);
        _meterListener.Start();

        using var host = await CreateHost(_meter);

        var response = await host.GetTestClient().GetAsync("testDouble?api-version=2023-08-29&api-version=2023-09-01");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        void OnMeasurementRecorded(Instrument instrument, long measurement, ReadOnlySpan<KeyValuePair<string, object?>> tags, object? state)
        {
            var expectedTags = new KeyValuePair<string, object?>[]
            {
                new KeyValuePair<string, object?>("api_version", "2023-08-29,2023-09-01"),
                new KeyValuePair<string, object?>("CustomerResourceId", "TestCustomerResourceId"),
                new KeyValuePair<string, object?>("LocationId", "ms-loc://az/public/West US 3"),
                new KeyValuePair<string, object?>("Operation", "GET TestDouble"),
                new KeyValuePair<string, object?>("Status", "Ok"),
                new KeyValuePair<string, object?>("HttpStatusCode", 200),
            };

            ValidateMetrics(instrument, measurement, tags, expectedTags);
        }

        _callbackCalled.Should().BeTrue();
    }


    [Fact]
    public async Task SLI_Metrics_is_emitted_with_neutral_API_versions_on_Controller()
    {
        _meterListener.SetMeasurementEventCallback<long>(OnMeasurementRecorded);
        _meterListener.Start();

        using var host = await CreateHost(_meter);

        var response = await host.GetTestClient().GetAsync("testNeutral");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        void OnMeasurementRecorded(Instrument instrument, long measurement, ReadOnlySpan<KeyValuePair<string, object?>> tags, object? state)
        {
            var expectedTags = new KeyValuePair<string, object?>[]
            {
                new KeyValuePair<string, object?>("api_version", "Neutral"),
                new KeyValuePair<string, object?>("CustomerResourceId", "TestCustomerResourceId"),
                new KeyValuePair<string, object?>("LocationId", "ms-loc://az/public/West US 3"),
                new KeyValuePair<string, object?>("Operation", "GET TestNeutral"),
                new KeyValuePair<string, object?>("Status", "Ok"),
                new KeyValuePair<string, object?>("HttpStatusCode", 200),
            };

            ValidateMetrics(instrument, measurement, tags, expectedTags);
        }

        _callbackCalled.Should().BeTrue();
    }

    [Fact]
    public async Task SLI_Metrics_is_emitted_with_default_API_version()
    {
        _meterListener.SetMeasurementEventCallback<long>(OnMeasurementRecorded);
        _meterListener.Start();

        using var host = await CreateHostWithDefaultApiVersion(_meter);

        var response = await host.GetTestClient().GetAsync("testSingle");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        void OnMeasurementRecorded(Instrument instrument, long measurement, ReadOnlySpan<KeyValuePair<string, object?>> tags, object? state)
        {
            var expectedTags = new KeyValuePair<string, object?>[]
            {
                new KeyValuePair<string, object?>("api_version", "2023-08-29"),
                new KeyValuePair<string, object?>("CustomerResourceId", "TestCustomerResourceId"),
                new KeyValuePair<string, object?>("LocationId", "ms-loc://az/public/West US 3"),
                new KeyValuePair<string, object?>("Operation", "GET TestSingle"),
                new KeyValuePair<string, object?>("Status", "Ok"),
                new KeyValuePair<string, object?>("HttpStatusCode", 200),
            };

            ValidateMetrics(instrument, measurement, tags, expectedTags);
        }

        _callbackCalled.Should().BeTrue();
    }

    private static async Task<IHost> CreateHost(Meter meter) =>
    await new HostBuilder()
        .ConfigureWebHost(webBuilder =>
        {
            webBuilder
                .UseTestServer()
                .ConfigureServices(services =>
                {
                    services.AddControllers();
                    services.AddApiVersioning(
                        options => options.ApiVersionReader = ApiVersionReader.Combine(
                            new QueryStringApiVersionReader(),
                            new HeaderApiVersionReader() { HeaderNames = { "api-version" } }))
                    .AddMvc();
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
                       .UseServiceLevelIndicatorWithApiVersioning()
                       .UseEndpoints(endpoints => endpoints.MapControllers());
                });
        })
        .StartAsync();

    private static async Task<IHost> CreateHostWithDefaultApiVersion(Meter meter) =>
    await new HostBuilder()
        .ConfigureWebHost(webBuilder =>
        {
            webBuilder
                .UseTestServer()
                .ConfigureServices(services =>
                {
                    services.AddControllers();
                    services.AddApiVersioning(options
                        => options.DefaultApiVersion = new ApiVersion(new DateOnly(2023, 8, 29)))
                    .AddMvc();
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
                       .UseServiceLevelIndicatorWithApiVersioning()
                       .UseEndpoints(endpoints => endpoints.MapControllers());
                });
        })
        .StartAsync();

    private void ValidateMetrics(Instrument instrument,
        long measurement,
        ReadOnlySpan<KeyValuePair<string, object?>> tags,
        KeyValuePair<string, object?>[] expectedTags)
    {
        _callbackCalled = true;
        instrument.Name.Should().Be("LatencySLI");
        instrument.Unit.Should().Be("ms");
        measurement.Should().BeGreaterOrEqualTo(1);
        _output.WriteLine($"Measurement {measurement}");
        tags.ToArray().Should().BeEquivalentTo(expectedTags);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposedValue)
        {
            if (disposing)
            {
                _meter.Dispose();
                _meterListener.Dispose();
            }

            _disposedValue = true;
        }
    }

    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
}
