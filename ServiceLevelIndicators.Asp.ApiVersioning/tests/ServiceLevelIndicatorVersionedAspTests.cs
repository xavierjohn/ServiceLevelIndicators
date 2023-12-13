namespace ServiceLevelIndicators.Asp.ApiVersioning.Tests;

using global::Asp.Versioning;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Diagnostics.Metrics;
using System.Net;
using Xunit.Abstractions;

public class ServiceLevelIndicatorVersionedAspTests : IDisposable
{
    private const int MillisecondsDelay = 200;
    private readonly Meter _meter;
    private readonly MeterListener _meterListener;
    private readonly ITestOutputHelper _output;
    private KeyValuePair<string, object?>[] _actualTags;
    private KeyValuePair<string, object?>[] _expectedTags;
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
        _meterListener.SetMeasurementEventCallback<long>(OnMeasurementRecorded);
        _meterListener.Start();

        _actualTags = Array.Empty<KeyValuePair<string, object?>>();
        _expectedTags = Array.Empty<KeyValuePair<string, object?>>();
    }

    [Fact]
    public async Task SLI_Metrics_is_emitted_with_API_version_as_query_parameter()
    {
        // Arrange
        _expectedTags = new KeyValuePair<string, object?>[]
        {
            new KeyValuePair<string, object?>("CustomerResourceId", "TestCustomerResourceId"),
            new KeyValuePair<string, object?>("LocationId", "ms-loc://az/public/West US 3"),
            new KeyValuePair<string, object?>("Operation", "GET TestSingle"),
            new KeyValuePair<string, object?>("activity.status_code", "Ok"),
            new KeyValuePair<string, object?>("http.request.method", "GET"),
            new KeyValuePair<string, object?>("http.response.status_code", 200),
            new KeyValuePair<string, object?>("http.api.version", "2023-08-29"),
        };
        using var host = await CreateHost();

        // Act
        var response = await host.GetTestClient().GetAsync("testSingle?api-version=2023-08-29");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        ValidateMetrics();
    }

    [Fact]
    public async Task SLI_Metrics_is_emitted_with_API_version_as_header()
    {
        // Arrange
        _expectedTags = new KeyValuePair<string, object?>[]
        {
                new KeyValuePair<string, object?>("CustomerResourceId", "TestCustomerResourceId"),
                new KeyValuePair<string, object?>("LocationId", "ms-loc://az/public/West US 3"),
                new KeyValuePair<string, object?>("Operation", "GET TestSingle"),
                new KeyValuePair<string, object?>("activity.status_code", "Ok"),
                new KeyValuePair<string, object?>("http.request.method", "GET"),
                new KeyValuePair<string, object?>("http.response.status_code", 200),
                new KeyValuePair<string, object?>("http.api.version", "2023-08-29"),
        };
        using var host = await CreateHost();
        var httpClient = host.GetTestClient();
        httpClient.DefaultRequestHeaders.Add("api-version", "2023-08-29");

        // Act
        var response = await httpClient.GetAsync("testSingle");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        ValidateMetrics();
    }

    [Fact]
    public async Task SLI_Metrics_is_emitted_with_neutral_API_version()
    {
        // Arrange
        _expectedTags = new KeyValuePair<string, object?>[]
        {
                new KeyValuePair<string, object?>("http.api.version", "Neutral"),
                new KeyValuePair<string, object?>("CustomerResourceId", "TestCustomerResourceId"),
                new KeyValuePair<string, object?>("LocationId", "ms-loc://az/public/West US 3"),
                new KeyValuePair<string, object?>("Operation", "GET TestNeutral"),
                new KeyValuePair<string, object?>("http.request.method", "GET"),
                new KeyValuePair<string, object?>("activity.status_code", "Ok"),
                new KeyValuePair<string, object?>("http.response.status_code", 200),
        };
        using var host = await CreateHost();

        // Act
        var response = await host.GetTestClient().GetAsync("testNeutral");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        ValidateMetrics();
    }

    [Fact]
    public async Task SLI_Metrics_is_emitted_with_default_API_version()
    {
        // Arrange
        _expectedTags = new KeyValuePair<string, object?>[]
        {
                new KeyValuePair<string, object?>("http.api.version", "2023-08-29"),
                new KeyValuePair<string, object?>("CustomerResourceId", "TestCustomerResourceId"),
                new KeyValuePair<string, object?>("LocationId", "ms-loc://az/public/West US 3"),
                new KeyValuePair<string, object?>("Operation", "GET TestSingle"),
                new KeyValuePair<string, object?>("activity.status_code", "Ok"),
                new KeyValuePair<string, object?>("http.request.method", "GET"),
                new KeyValuePair<string, object?>("http.response.status_code", 200),
        };
        using var host = await CreateHostWithDefaultApiVersion();

        // Act
        var response = await host.GetTestClient().GetAsync("testSingle");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        ValidateMetrics();
    }

    [Fact]
    public async Task Middleware_should_not_emit_metrics_for_nonexistent_route()
    {
        // Arrange
        using var host = await CreateHost();

        // Act
        var response = await host.GetTestClient().GetAsync("does-not-exist?api-version=2023-08-29");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Theory]
    [InlineData("testSingle", "api-version=invalid")]
    [InlineData("testDouble", "api-version=2023-08-29&api-version=2023-09-01")]
    public async Task SLI_Metrics_is_emitted_when_invalid_api_version(string route, string version)
    {
        // Arrange
        _expectedTags = new KeyValuePair<string, object?>[]
        {
                new KeyValuePair<string, object?>("http.api.version", string.Empty),
                new KeyValuePair<string, object?>("CustomerResourceId", "TestCustomerResourceId"),
                new KeyValuePair<string, object?>("LocationId", "ms-loc://az/public/West US 3"),
                new KeyValuePair<string, object?>("Operation", "GET /" + route),
                new KeyValuePair<string, object?>("activity.status_code", "Unset"),
                new KeyValuePair<string, object?>("http.request.method", "GET"),
                new KeyValuePair<string, object?>("http.response.status_code", 400),
        };
        var routeWithVersion = route + "?" + version;
        using var host = await CreateHost();

        // Act
        var response = await host.GetTestClient().GetAsync(routeWithVersion);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        ValidateMetrics();
    }

    private async Task<IHost> CreateHost() =>
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
                        options.Meter = _meter;
                        options.CustomerResourceId = "TestCustomerResourceId";
                        options.LocationId = ServiceLevelIndicator.CreateLocationId("public", "West US 3");
                    });
                })
                .Configure(app =>
                {
                    app.UseRouting()
                       .UseServiceLevelIndicatorWithApiVersioning()
                       .Use(async (context, next) =>
                        {
                            await Task.Delay(MillisecondsDelay);
                            await next(context);
                        })
                       .UseEndpoints(endpoints => endpoints.MapControllers());
                });
        })
        .StartAsync();

    private async Task<IHost> CreateHostWithDefaultApiVersion() =>
    await new HostBuilder()
        .ConfigureWebHost(webBuilder =>
        {
            webBuilder
                .UseTestServer()
                .ConfigureServices(services =>
                {
                    services.AddControllers();
                    services.AddApiVersioning(options
                        =>
                    {
                        options.AssumeDefaultVersionWhenUnspecified = true;
                        options.DefaultApiVersion = new ApiVersion(new DateOnly(2023, 8, 29));
                    })
                    .AddMvc();
                    services.AddServiceLevelIndicator(options =>
                    {
                        options.Meter = _meter;
                        options.CustomerResourceId = "TestCustomerResourceId";
                        options.LocationId = ServiceLevelIndicator.CreateLocationId("public", "West US 3");
                    });
                })
                .Configure(app =>
                {
                    app.UseRouting()
                       .UseServiceLevelIndicatorWithApiVersioning()
                       .Use(async (context, next) =>
                       {
                           await Task.Delay(MillisecondsDelay);
                           await next(context);
                       })
                       .UseEndpoints(endpoints => endpoints.MapControllers());
                });
        })
        .StartAsync();

    private void ValidateMetrics()
    {
        _callbackCalled.Should().BeTrue();
        _actualTags.Should().BeEquivalentTo(_expectedTags);
    }

    private void OnMeasurementRecorded(Instrument instrument, long measurement, ReadOnlySpan<KeyValuePair<string, object?>> tags, object? state)
    {
        _actualTags = tags.ToArray();
        _callbackCalled = true;

        _output.WriteLine($"Measurement {measurement}");
        instrument.Name.Should().Be("LatencySLI");
        instrument.Unit.Should().Be("ms");
        measurement.Should().BeInRange(MillisecondsDelay - 10, MillisecondsDelay + 400);
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
