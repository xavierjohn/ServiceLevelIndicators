﻿namespace ServiceLevelIndicators.Asp.Tests;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Diagnostics.Metrics;
using System.Net;
using Xunit.Abstractions;

public class ServiceLevelIndicatorAspTests : IDisposable
{
    private const int MillisecondsDelay = 200;
    private readonly Meter _meter;
    private readonly MeterListener _meterListener;
    private readonly ITestOutputHelper _output;
    private bool _callbackCalled;
    private bool _disposedValue;

    public ServiceLevelIndicatorAspTests(ITestOutputHelper output)
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
    public async Task SLI_Metrics_is_emitted_for_successful_API_call()
    {
        _meterListener.SetMeasurementEventCallback<long>(OnMeasurementRecorded);
        _meterListener.Start();

        using var host = await CreateHostWithSli(_meter);

        var response = await host.GetTestClient().GetAsync("test");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        void OnMeasurementRecorded(Instrument instrument, long measurement, ReadOnlySpan<KeyValuePair<string, object?>> tags, object? state)
        {
            var expectedTags = new KeyValuePair<string, object?>[]
            {
                new KeyValuePair<string, object?>("CustomerResourceId", "TestCustomerResourceId"),
                new KeyValuePair<string, object?>("LocationId", "ms-loc://az/public/West US 3"),
                new KeyValuePair<string, object?>("Operation", "GET Test"),
                new KeyValuePair<string, object?>("activity.status_code", "Ok"),
                new KeyValuePair<string, object?>("http.request.method", "GET"),
                new KeyValuePair<string, object?>("http.response.status_code", 200),
            };

            ValidateMetrics(instrument, measurement, tags, expectedTags);
        }

        _callbackCalled.Should().BeTrue();
    }

    [Fact]
    public async Task SLI_Metrics_is_emitted_for_successful_POST_API_call()
    {
        _meterListener.SetMeasurementEventCallback<long>(OnMeasurementRecorded);
        _meterListener.Start();

        using var host = await CreateHostWithSli(_meter);

        var response = await host.GetTestClient().PostAsync("test", new StringContent("Hi"));
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        void OnMeasurementRecorded(Instrument instrument, long measurement, ReadOnlySpan<KeyValuePair<string, object?>> tags, object? state)
        {
            var expectedTags = new KeyValuePair<string, object?>[]
            {
                new KeyValuePair<string, object?>("CustomerResourceId", "TestCustomerResourceId"),
                new KeyValuePair<string, object?>("LocationId", "ms-loc://az/public/West US 3"),
                new KeyValuePair<string, object?>("Operation", "POST Test"),
                new KeyValuePair<string, object?>("activity.status_code", "Ok"),
                new KeyValuePair<string, object?>("http.request.method", "POST"),
                new KeyValuePair<string, object?>("http.response.status_code", 200),
            };

            ValidateMetrics(instrument, measurement, tags, expectedTags);
        }

        _callbackCalled.Should().BeTrue();
    }

    [Fact]
    public async Task SLI_Metrics_is_emitted_for_failed_API_call()
    {
        _meterListener.SetMeasurementEventCallback<long>(OnMeasurementRecorded);
        _meterListener.Start();

        using var host = await CreateHostWithSli(_meter);

        var response = await host.GetTestClient().GetAsync("test/bad_request");
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        void OnMeasurementRecorded(Instrument instrument, long measurement, ReadOnlySpan<KeyValuePair<string, object?>> tags, object? state)
        {
            var expectedTags = new KeyValuePair<string, object?>[]
            {
                new KeyValuePair<string, object?>("CustomerResourceId", "TestCustomerResourceId"),
                new KeyValuePair<string, object?>("LocationId", "ms-loc://az/public/West US 3"),
                new KeyValuePair<string, object?>("Operation", "GET Test/bad_request"),
                new KeyValuePair<string, object?>("activity.status_code", "Unset"),
                new KeyValuePair<string, object?>("http.request.method", "GET"),
                new KeyValuePair<string, object?>("http.response.status_code", 400),
            };

            ValidateMetrics(instrument, measurement, tags, expectedTags);
        }

        _callbackCalled.Should().BeTrue();
    }

    private void ValidateMetrics(Instrument instrument, long measurement, ReadOnlySpan<KeyValuePair<string, object?>> tags, KeyValuePair<string, object?>[] expectedTags)
    {
        _callbackCalled = true;
        instrument.Name.Should().Be("LatencySLI");
        instrument.Unit.Should().Be("ms");
        measurement.Should().BeInRange(MillisecondsDelay - 10, MillisecondsDelay + 400);
        _output.WriteLine($"Measurement {measurement}");
        tags.ToArray().Should().BeEquivalentTo(expectedTags);
    }

    [Fact]
    public async Task Override_Operation_name()
    {
        _meterListener.SetMeasurementEventCallback<long>(OnMeasurementRecorded);
        _meterListener.Start();

        using var host = await CreateHostWithSli(_meter);

        var response = await host.GetTestClient().GetAsync("test/operation");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        void OnMeasurementRecorded(Instrument instrument, long measurement, ReadOnlySpan<KeyValuePair<string, object?>> tags, object? state)
        {
            var expectedTags = new KeyValuePair<string, object?>[]
            {
                new KeyValuePair<string, object?>("CustomerResourceId", "TestCustomerResourceId"),
                new KeyValuePair<string, object?>("LocationId", "ms-loc://az/public/West US 3"),
                new KeyValuePair<string, object?>("Operation", "TestOperation"),
                new KeyValuePair<string, object?>("activity.status_code", "Ok"),
                new KeyValuePair<string, object?>("http.request.method", "GET"),
                new KeyValuePair<string, object?>("http.response.status_code", 200),
            };

            ValidateMetrics(instrument, measurement, tags, expectedTags);
        }

        _callbackCalled.Should().BeTrue();
    }

    [Fact]
    public async Task Override_CustomerResourceId()
    {
        _meterListener.SetMeasurementEventCallback<long>(OnMeasurementRecorded);
        _meterListener.Start();

        using var host = await CreateHostWithSli(_meter);

        var response = await host.GetTestClient().GetAsync("test/customer_resourceid/myId");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        void OnMeasurementRecorded(Instrument instrument, long measurement, ReadOnlySpan<KeyValuePair<string, object?>> tags, object? state)
        {
            var expectedTags = new KeyValuePair<string, object?>[]
            {
                new KeyValuePair<string, object?>("CustomerResourceId", "myId"),
                new KeyValuePair<string, object?>("LocationId", "ms-loc://az/public/West US 3"),
                new KeyValuePair<string, object?>("Operation", "GET Test/customer_resourceid/{id}"),
                new KeyValuePair<string, object?>("activity.status_code", "Ok"),
                new KeyValuePair<string, object?>("http.request.method", "GET"),
                new KeyValuePair<string, object?>("http.response.status_code", 200),
            };

            ValidateMetrics(instrument, measurement, tags, expectedTags);
        }

        _callbackCalled.Should().BeTrue();
    }

    [Fact]
    public async Task Add_custom_SLI_attribute()
    {
        _meterListener.SetMeasurementEventCallback<long>(OnMeasurementRecorded);
        _meterListener.Start();

        using var host = await CreateHostWithSli(_meter);

        var response = await host.GetTestClient().GetAsync("test/custom_attribute/Mickey");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        void OnMeasurementRecorded(Instrument instrument, long measurement, ReadOnlySpan<KeyValuePair<string, object?>> tags, object? state)
        {
            var expectedTags = new KeyValuePair<string, object?>[]
            {
                new KeyValuePair<string, object?>("CustomerResourceId", "TestCustomerResourceId"),
                new KeyValuePair<string, object?>("LocationId", "ms-loc://az/public/West US 3"),
                new KeyValuePair<string, object?>("Operation", "GET Test/custom_attribute/{value}"),
                new KeyValuePair<string, object?>("activity.status_code", "Ok"),
                new KeyValuePair<string, object?>("http.request.method", "GET"),
                new KeyValuePair<string, object?>("http.response.status_code", 200),
                new KeyValuePair<string, object?>("CustomAttribute", "Mickey"),
            };

            ValidateMetrics(instrument, measurement, tags, expectedTags);
        }

        _callbackCalled.Should().BeTrue();
    }

    [Fact]
    public async Task When_automatically_emit_SLI_is_Off_do_not_send_SLI()
    {
        _meterListener.SetMeasurementEventCallback<long>(OnMeasurementRecorded);
        _meterListener.Start();

        using var host = await CreateHostWithoutAutomaticSli();

        var response = await host.GetTestClient().GetAsync("test");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        void OnMeasurementRecorded(Instrument instrument, long measurement, ReadOnlySpan<KeyValuePair<string, object?>> tags, object? state)
        {
            var expectedTags = new KeyValuePair<string, object?>[]
            {
                new KeyValuePair<string, object?>("CustomerResourceId", "TestCustomerResourceId"),
            };

            ValidateMetrics(instrument, measurement, tags, expectedTags);
        }

        _callbackCalled.Should().BeFalse();
    }

    [Fact]
    public async Task When_automatically_emit_SLI_is_Off_X2C_send_SLI_using_attribute()
    {
        _meterListener.SetMeasurementEventCallback<long>(OnMeasurementRecorded);
        _meterListener.Start();

        using var host = await CreateHostWithoutAutomaticSli();

        var response = await host.GetTestClient().GetAsync("test/send_sli");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        void OnMeasurementRecorded(Instrument instrument, long measurement, ReadOnlySpan<KeyValuePair<string, object?>> tags, object? state)
        {
            var expectedTags = new KeyValuePair<string, object?>[]
            {
                new KeyValuePair<string, object?>("CustomerResourceId", "TestCustomerResourceId"),
                new KeyValuePair<string, object?>("LocationId", "ms-loc://az/public/West US 3"),
                new KeyValuePair<string, object?>("Operation", "GET Test/send_sli"),
                new KeyValuePair<string, object?>("http.request.method", "GET"),
                new KeyValuePair<string, object?>("activity.status_code", "Ok"),
                new KeyValuePair<string, object?>("http.response.status_code", 200),
            };

            ValidateMetrics(instrument, measurement, tags, expectedTags);
        }

        _callbackCalled.Should().BeTrue();
    }

    [Fact]
    public async Task GetMeasuredOperationLatency_will_throw_if_route_does_not_emit_SLI()
    {
        using var host = await CreateHostWithoutSli();

        var response = await host.GetTestClient().GetAsync("test");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        Func<Task> getMeasuredOperationLatency = () => host.GetTestClient().GetAsync("test/custom_attribute/Mickey");

        await getMeasuredOperationLatency.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task TryGetMeasuredOperationLatency_will_return_false_if_route_does_not_emit_SLI()
    {
        using var host = await CreateHostWithoutSli();

        var response = await host.GetTestClient().GetAsync("test/try_get_measured_operation_latency/Donald");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();

        content.Should().Be("false");
    }

    [Fact]
    public async Task TryGetMeasuredOperationLatency_will_return_true_if_route_emits_SLI()
    {
        _meterListener.SetMeasurementEventCallback<long>(OnMeasurementRecorded);
        _meterListener.Start();

        using var host = await CreateHostWithSli(_meter);

        var response = await host.GetTestClient().GetAsync("test/try_get_measured_operation_latency/Goofy");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();

        content.Should().Be("true");

        void OnMeasurementRecorded(Instrument instrument, long measurement, ReadOnlySpan<KeyValuePair<string, object?>> tags, object? state)
        {
            var expectedTags = new KeyValuePair<string, object?>[]
            {
                new KeyValuePair<string, object?>("CustomerResourceId", "TestCustomerResourceId"),
                new KeyValuePair<string, object?>("LocationId", "ms-loc://az/public/West US 3"),
                new KeyValuePair<string, object?>("Operation", "GET Test/try_get_measured_operation_latency/{value}"),
                new KeyValuePair<string, object?>("activity.status_code", "Ok"),
                new KeyValuePair<string, object?>("http.request.method", "GET"),
                new KeyValuePair<string, object?>("http.response.status_code", 200),
                new KeyValuePair<string, object?>("CustomAttribute", "Goofy"),
            };

            ValidateMetrics(instrument, measurement, tags, expectedTags);
        }

        _callbackCalled.Should().BeTrue();
    }

    private static async Task<IHost> CreateHostWithSli(Meter meter) =>
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
                           .Use(async (context, next) =>
                           {
                               await Task.Delay(MillisecondsDelay);
                               await next(context);
                           })
                           .UseEndpoints(endpoints => endpoints.MapControllers());
                    });
            })
            .StartAsync();
    private async Task<IHost> CreateHostWithoutAutomaticSli()
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
                            options.Meter = _meter;
                            options.CustomerResourceId = "TestCustomerResourceId";
                            options.LocationId = ServiceLevelIndicator.CreateLocationId("public", "West US 3");
                            options.AutomaticallyEmitted = false;
                        });
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

    private static async Task<IHost> CreateHostWithoutSli()
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
