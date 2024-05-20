namespace ServiceLevelIndicators.Asp.Tests;
using Microsoft.AspNetCore.TestHost;
using Newtonsoft.Json.Linq;
using System;
using System.Diagnostics.Metrics;
using System.Net;
using Xunit.Abstractions;

public class ServiceLevelIndicatorAspTests : IDisposable
{
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

        using var host = await TestHostBuilder.CreateHostWithSli(_meter);

        var response = await host.GetTestClient().GetAsync("test");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        void OnMeasurementRecorded(Instrument instrument, long measurement, ReadOnlySpan<KeyValuePair<string, object?>> tags, object? state)
        {
            var expectedTags = new KeyValuePair<string, object?>[]
            {
                new("CustomerResourceId", "TestCustomerResourceId"),
                new("LocationId", "ms-loc://az/public/West US 3"),
                new("Operation", "GET Test"),
                new("activity.status.code", "Ok"),
                new("http.response.status.code", 200),
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

        using var host = await TestHostBuilder.CreateHostWithSli(_meter);

        var response = await host.GetTestClient().PostAsync("test", new StringContent("Hi"));
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        void OnMeasurementRecorded(Instrument instrument, long measurement, ReadOnlySpan<KeyValuePair<string, object?>> tags, object? state)
        {
            var expectedTags = new KeyValuePair<string, object?>[]
            {
                new("CustomerResourceId", "TestCustomerResourceId"),
                new("LocationId", "ms-loc://az/public/West US 3"),
                new("Operation", "POST Test"),
                new("activity.status.code", "Ok"),
                new("http.response.status.code", 200),
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

        using var host = await TestHostBuilder.CreateHostWithSli(_meter);

        var response = await host.GetTestClient().GetAsync("test/bad_request");
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        void OnMeasurementRecorded(Instrument instrument, long measurement, ReadOnlySpan<KeyValuePair<string, object?>> tags, object? state)
        {
            var expectedTags = new KeyValuePair<string, object?>[]
            {
                new("CustomerResourceId", "TestCustomerResourceId"),
                new("LocationId", "ms-loc://az/public/West US 3"),
                new("Operation", "GET Test/bad_request"),
                new("activity.status.code", "Unset"),
                new("http.response.status.code", 400),
            };

            ValidateMetrics(instrument, measurement, tags, expectedTags);
        }

        _callbackCalled.Should().BeTrue();
    }

    [Fact]
    public async Task SLI_Metrics_is_emitted_with_enriched_data()
    {
        _meterListener.SetMeasurementEventCallback<long>(OnMeasurementRecorded);
        _meterListener.Start();
        HttpRequestMessage request = new(HttpMethod.Get, "test");
        request.Headers.Add("from", "xavier@somewhere.com");

        using var host = await TestHostBuilder.CreateHostWithSliEnriched(_meter);

        var response = await host.GetTestClient().SendAsync(request);
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        void OnMeasurementRecorded(Instrument instrument, long measurement, ReadOnlySpan<KeyValuePair<string, object?>> tags, object? state)
        {
            var expectedTags = new KeyValuePair<string, object?>[]
            {
                new("CustomerResourceId", "xavier@somewhere.com"),
                new("LocationId", "ms-loc://az/public/West US 3"),
                new("Operation", "GET Test"),
                new("activity.status.code", "Ok"),
                new("http.request.method", "GET"),
                new("http.response.status.code", 200),
                new("foo", "bar"),
                new("test", "again"),
                new("enrichAsync", "async"),
            };

            ValidateMetrics(instrument, measurement, tags, expectedTags);
        }

        _callbackCalled.Should().BeTrue();
    }

    [Fact]
    public async Task Override_Operation_name()
    {
        _meterListener.SetMeasurementEventCallback<long>(OnMeasurementRecorded);
        _meterListener.Start();

        using var host = await TestHostBuilder.CreateHostWithSli(_meter);

        var response = await host.GetTestClient().GetAsync("test/operation");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        void OnMeasurementRecorded(Instrument instrument, long measurement, ReadOnlySpan<KeyValuePair<string, object?>> tags, object? state)
        {
            var expectedTags = new KeyValuePair<string, object?>[]
            {
                new("CustomerResourceId", "TestCustomerResourceId"),
                new("LocationId", "ms-loc://az/public/West US 3"),
                new("Operation", "TestOperation"),
                new("activity.status.code", "Ok"),
                new("http.response.status.code", 200),
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

        using var host = await TestHostBuilder.CreateHostWithSli(_meter);

        var response = await host.GetTestClient().GetAsync("test/customer_resourceid/myId");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        void OnMeasurementRecorded(Instrument instrument, long measurement, ReadOnlySpan<KeyValuePair<string, object?>> tags, object? state)
        {
            var expectedTags = new KeyValuePair<string, object?>[]
            {
                new("CustomerResourceId", "myId"),
                new("LocationId", "ms-loc://az/public/West US 3"),
                new("Operation", "GET Test/customer_resourceid/{id}"),
                new("activity.status.code", "Ok"),
                new("http.response.status.code", 200),
            };

            ValidateMetrics(instrument, measurement, tags, expectedTags);
        }

        _callbackCalled.Should().BeTrue();
    }

    [Fact]
    public async Task CustomAttribute_is_added_to_SLI_dimension()
    {
        _meterListener.SetMeasurementEventCallback<long>(OnMeasurementRecorded);
        _meterListener.Start();

        using var host = await TestHostBuilder.CreateHostWithSli(_meter);

        var response = await host.GetTestClient().GetAsync("test/custom_attribute/Mickey");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        void OnMeasurementRecorded(Instrument instrument, long measurement, ReadOnlySpan<KeyValuePair<string, object?>> tags, object? state)
        {
            var expectedTags = new KeyValuePair<string, object?>[]
            {
                new("CustomerResourceId", "TestCustomerResourceId"),
                new("LocationId", "ms-loc://az/public/West US 3"),
                new("Operation", "GET Test/custom_attribute/{value}"),
                new("activity.status.code", "Ok"),
                new("http.response.status.code", 200),
                new("CustomAttribute", "Mickey"),
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

        using var host = await TestHostBuilder.CreateHostWithoutAutomaticSli(_meter);

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

        using var host = await TestHostBuilder.CreateHostWithoutAutomaticSli(_meter);

        var response = await host.GetTestClient().GetAsync("test/send_sli");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        void OnMeasurementRecorded(Instrument instrument, long measurement, ReadOnlySpan<KeyValuePair<string, object?>> tags, object? state)
        {
            var expectedTags = new KeyValuePair<string, object?>[]
            {
                new("CustomerResourceId", "TestCustomerResourceId"),
                new("LocationId", "ms-loc://az/public/West US 3"),
                new("Operation", "GET Test/send_sli"),
                new("activity.status.code", "Ok"),
                new("http.response.status.code", 200),
            };

            ValidateMetrics(instrument, measurement, tags, expectedTags);
        }

        _callbackCalled.Should().BeTrue();
    }

    [Fact]
    public async Task GetMeasuredOperation_will_throw_if_route_does_not_emit_SLI()
    {
        using var host = await TestHostBuilder.CreateHostWithoutSli();

        var response = await host.GetTestClient().GetAsync("test");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        Func<Task> getMeasuredOperationLatency = () => host.GetTestClient().GetAsync("test/custom_attribute/Mickey");

        await getMeasuredOperationLatency.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task TryGetMeasuredOperation_will_return_false_if_route_does_not_emit_SLI()
    {
        using var host = await TestHostBuilder.CreateHostWithoutSli();

        var response = await host.GetTestClient().GetAsync("test/try_get_measured_operation_latency/Donald");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();

        content.Should().Be("false");
    }

    [Fact]
    public async Task TryGetMeasuredOperation_will_return_true_if_route_emits_SLI()
    {
        _meterListener.SetMeasurementEventCallback<long>(OnMeasurementRecorded);
        _meterListener.Start();

        using var host = await TestHostBuilder.CreateHostWithSli(_meter);

        var response = await host.GetTestClient().GetAsync("test/try_get_measured_operation_latency/Goofy");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();

        content.Should().Be("true");

        void OnMeasurementRecorded(Instrument instrument, long measurement, ReadOnlySpan<KeyValuePair<string, object?>> tags, object? state)
        {
            var expectedTags = new KeyValuePair<string, object?>[]
            {
                new("CustomerResourceId", "TestCustomerResourceId"),
                new("LocationId", "ms-loc://az/public/West US 3"),
                new("Operation", "GET Test/try_get_measured_operation_latency/{value}"),
                new("activity.status.code", "Ok"),
                new("http.response.status.code", 200),
                new("CustomAttribute", "Goofy"),
            };

            ValidateMetrics(instrument, measurement, tags, expectedTags);
        }

        _callbackCalled.Should().BeTrue();
    }

    [Fact]
    public async Task SLI_Measure_is_emitted()
    {
        _meterListener.SetMeasurementEventCallback<long>(OnMeasurementRecorded);
        _meterListener.Start();

        using var host = await TestHostBuilder.CreateHostWithSli(_meter);

        var response = await host.GetTestClient().GetAsync("test/name/Xavier/Jon/25");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        void OnMeasurementRecorded(Instrument instrument, long measurement, ReadOnlySpan<KeyValuePair<string, object?>> tags, object? state)
        {
            var expectedTags = new KeyValuePair<string, object?>[]
            {
                new("CustomerResourceId", "Jon"),
                new("first", "Xavier"),
                new("age", "25"),
                new("LocationId", "ms-loc://az/public/West US 3"),
                new("Operation", "GET Test/name/{first}/{surname}/{age}"),
                new("activity.status.code", "Ok"),
                new("http.response.status.code", 200),
            };

            ValidateMetrics(instrument, measurement, tags, expectedTags);
        }

        _callbackCalled.Should().BeTrue();
    }

    [Fact]
    public async Task SLI_multiple_CustomerResourceId_will_fail()
    {
        using var host = await TestHostBuilder.CreateHostWithSli(_meter);

        Func<Task> act = () => host.GetTestClient().GetAsync("test/multiple_customer_resource_id/Xavier/Jon");
        await act.Should().ThrowAsync<ArgumentException>();
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

    private void ValidateMetrics(Instrument instrument, long measurement, ReadOnlySpan<KeyValuePair<string, object?>> tags, KeyValuePair<string, object?>[] expectedTags)
    {
        _callbackCalled = true;
        instrument.Name.Should().Be("ServiceLevelIndicator");
        instrument.Unit.Should().Be("ms");
        measurement.Should().BeInRange(TestHostBuilder.MillisecondsDelay - 10, TestHostBuilder.MillisecondsDelay + 400);
        _output.WriteLine($"Measurement {measurement}");
        tags.ToArray().Should().BeEquivalentTo(expectedTags);
    }

}
