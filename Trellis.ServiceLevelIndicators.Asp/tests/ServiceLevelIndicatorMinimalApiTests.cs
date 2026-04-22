namespace Trellis.ServiceLevelIndicators.Asp.Tests;

using System;
using System.Diagnostics.Metrics;
using System.Net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

public class ServiceLevelIndicatorMinimalApiTests : IDisposable
{
    private const int MillisecondsDelay = 200;
    private readonly Meter _meter;
    private readonly MeterListener _meterListener;
    private readonly ITestOutputHelper _output;
    private KeyValuePair<string, object?>[] _actualTags = [];
    private Instrument? _instrument;
    private long _measurement;
    private bool _callbackCalled;
    private bool _disposedValue;

    public ServiceLevelIndicatorMinimalApiTests(ITestOutputHelper output)
    {
        _output = output;
        const string MeterName = "SliMinApiTestMeter";
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
    }

    [Fact]
    public async Task SLI_Metrics_is_emitted_for_minimal_api_get()
    {
        // Arrange
        using var host = await CreateMinimalApiHost();

        // Act
        var response = await host.GetTestClient().GetAsync("hello", TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var expectedTags = new KeyValuePair<string, object?>[]
        {
            new("CustomerResourceId", "TestCustomerResourceId"),
            new("LocationId", "ms-loc://az/public/West US 3"),
            new("Operation", "GET /hello"),
            new("activity.status.code", "Ok"),
            new("http.response.status.code", 200),
        };

        ValidateMetrics(expectedTags);
    }

    [Fact]
    public async Task SLI_Metrics_is_emitted_with_custom_operation_name()
    {
        // Arrange
        using var host = await CreateMinimalApiHost();

        // Act
        var response = await host.GetTestClient().GetAsync("custom-operation", TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var expectedTags = new KeyValuePair<string, object?>[]
        {
            new("CustomerResourceId", "TestCustomerResourceId"),
            new("LocationId", "ms-loc://az/public/West US 3"),
            new("Operation", "CustomOp"),
            new("activity.status.code", "Ok"),
            new("http.response.status.code", 200),
        };

        ValidateMetrics(expectedTags);
    }

    [Fact]
    public async Task SLI_Metrics_is_emitted_with_customer_resource_id_from_route()
    {
        // Arrange
        using var host = await CreateMinimalApiHost();

        // Act
        var response = await host.GetTestClient().GetAsync("resource/myResourceId", TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var expectedTags = new KeyValuePair<string, object?>[]
        {
            new("CustomerResourceId", "myResourceId"),
            new("LocationId", "ms-loc://az/public/West US 3"),
            new("Operation", "GET /resource/myResourceId"),
            new("activity.status.code", "Ok"),
            new("http.response.status.code", 200),
        };

        ValidateMetrics(expectedTags);
    }

    [Fact]
    public async Task SLI_Metrics_is_emitted_with_measure_attribute()
    {
        // Arrange
        using var host = await CreateMinimalApiHost();

        // Act
        var response = await host.GetTestClient().GetAsync("measured/items/Widget", TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var expectedTags = new KeyValuePair<string, object?>[]
        {
            new("name", "Widget"),
            new("CustomerResourceId", "TestCustomerResourceId"),
            new("LocationId", "ms-loc://az/public/West US 3"),
            new("Operation", "GET /measured/items/Widget"),
            new("activity.status.code", "Ok"),
            new("http.response.status.code", 200),
        };

        ValidateMetrics(expectedTags);
    }

    [Fact]
    public async Task SLI_Metrics_not_emitted_when_AddServiceLevelIndicator_not_called()
    {
        // Arrange
        using var host = await CreateMinimalApiHost();

        // Act
        var response = await host.GetTestClient().GetAsync("no-sli", TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        _callbackCalled.Should().BeFalse();
    }

    [Fact]
    public async Task SLI_Metrics_is_emitted_with_enrichment_for_minimal_api()
    {
        // Arrange
        using var host = await CreateMinimalApiHostWithEnrichment();

        // Act
        var response = await host.GetTestClient().GetAsync("hello", TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var expectedTags = new KeyValuePair<string, object?>[]
        {
            new("CustomerResourceId", "TestCustomerResourceId"),
            new("LocationId", "ms-loc://az/public/West US 3"),
            new("Operation", "GET /hello"),
            new("activity.status.code", "Ok"),
            new("http.response.status.code", 200),
            new("http.request.method", "GET"),
        };

        ValidateMetrics(expectedTags);
    }

    private async Task<IHost> CreateMinimalApiHost() =>
        await new HostBuilder()
            .ConfigureWebHost(webBuilder => webBuilder
                .UseTestServer()
                .ConfigureServices(services =>
                {
                    services.AddRouting();
                    services.AddServiceLevelIndicator(options =>
                    {
                        options.Meter = _meter;
                        options.CustomerResourceId = "TestCustomerResourceId";
                        options.LocationId = ServiceLevelIndicator.CreateLocationId("public", "West US 3");
                        options.AutomaticallyEmitted = false;
                    });
                })
                .Configure(app =>
                {
                    app.UseRouting();
                    app.UseServiceLevelIndicator();
                    app.Use(async (context, next) =>
                    {
                        await Task.Delay(MillisecondsDelay);
                        await next(context);
                    });
                    app.UseEndpoints(endpoints =>
                    {
                        endpoints.MapGet("/hello", () => "Hello World!")
                            .AddServiceLevelIndicator();

                        endpoints.MapGet("/custom-operation", () => "Custom!")
                            .AddServiceLevelIndicator("CustomOp");

                        endpoints.MapGet("/resource/{id}", ([CustomerResourceId] string id) => $"Resource {id}")
                            .AddServiceLevelIndicator();

                        endpoints.MapGet("/measured/items/{name}", ([Measure] string name) => $"Item {name}")
                            .AddServiceLevelIndicator();

                        endpoints.MapGet("/no-sli", () => "No SLI");
                    });
                }))
            .StartAsync();

    private async Task<IHost> CreateMinimalApiHostWithEnrichment() =>
        await new HostBuilder()
            .ConfigureWebHost(webBuilder => webBuilder
                .UseTestServer()
                .ConfigureServices(services =>
                {
                    services.AddRouting();
                    services.AddServiceLevelIndicator(options =>
                    {
                        options.Meter = _meter;
                        options.CustomerResourceId = "TestCustomerResourceId";
                        options.LocationId = ServiceLevelIndicator.CreateLocationId("public", "West US 3");
                        options.AutomaticallyEmitted = false;
                    })
                    .AddHttpMethod();
                })
                .Configure(app =>
                {
                    app.UseRouting();
                    app.UseServiceLevelIndicator();
                    app.Use(async (context, next) =>
                    {
                        await Task.Delay(MillisecondsDelay);
                        await next(context);
                    });
                    app.UseEndpoints(endpoints =>
                        endpoints.MapGet("/hello", () => "Hello World!")
                            .AddServiceLevelIndicator());
                }))
            .StartAsync();

    private void OnMeasurementRecorded(Instrument instrument, long measurement, ReadOnlySpan<KeyValuePair<string, object?>> tags, object? state)
    {
        _callbackCalled = true;
        _instrument = instrument;
        _measurement = measurement;
        _actualTags = tags.ToArray();
        _output.WriteLine($"Measurement {measurement}");
    }

    private void ValidateMetrics(KeyValuePair<string, object?>[] expectedTags)
    {
        _callbackCalled.Should().BeTrue();
        _instrument!.Name.Should().Be("operation.duration");
        _instrument.Unit.Should().Be("ms");
        _measurement.Should().BeInRange(MillisecondsDelay - 10, MillisecondsDelay + 400);
        _actualTags.Should().BeEquivalentTo(expectedTags);
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
