﻿namespace ServiceLevelIndicators.Asp.ApiVersioning.Tests;

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
    public async Task Default_SLI_Metrics_is_emitted()
    {
        _meterListener.SetMeasurementEventCallback<long>(OnMeasurementRecorded);
        _meterListener.Start();

        using var host = await CreateHost(_meter);

        var response = await host.GetTestClient().GetAsync("test");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        void OnMeasurementRecorded(Instrument instrument, long measurement, ReadOnlySpan<KeyValuePair<string, object?>> tags, object? state)
        {
            var expectedTags = new KeyValuePair<string, object?>[]
            {
                new KeyValuePair<string, object?>("CustomerResourceId", "TestCustomerResourceId"),
                new KeyValuePair<string, object?>("LocationId", "ms-loc://az/public/West US 3"),
                new KeyValuePair<string, object?>("Operation", "GET Test"),
                new KeyValuePair<string, object?>("Status", "Ok"),
                new KeyValuePair<string, object?>("HttpStatusCode", 200),
                new KeyValuePair<string, object?>("api-version", "2023-08-29"),
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
                    services.AddApiVersioning().AddMvc();
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
    private void ValidateMetrics(Instrument instrument, long measurement, ReadOnlySpan<KeyValuePair<string, object?>> tags, KeyValuePair<string, object?>[] expectedTags)
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
