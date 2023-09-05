namespace ServiceLevelIndicators.Tests;
using System;
using System.Collections.Generic;
using System.Diagnostics.Metrics;
using Microsoft.Extensions.Options;
using Xunit.Abstractions;

public class ServiceLevelIndicatorTests : IDisposable
{
    private readonly Meter _meter;
    private readonly MeterListener _meterListener;
    private readonly ITestOutputHelper _output;
    private KeyValuePair<string, object?>[] _actualTags;
    private KeyValuePair<string, object?>[] _expectedTags;
    private bool _callbackCalled;
    private bool _disposedValue;
    private Instrument? _instrument;
    private long _measurement;

    public ServiceLevelIndicatorTests(ITestOutputHelper output)
    {
        _output = output;
        const string MeterName = "ServiceLevelIndicator";
        _meter = new(MeterName);
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
    public void Record_latency_test()
    {
        // Arrange
        var serviceId = Guid.NewGuid();
        var cloud = "Azure";
        var region = "EastUS";
        var zone = "Zone1";
        var customerResourceId = ServiceLevelIndicator.CreateCustomerResourceId(serviceId);
        var locationId = ServiceLevelIndicator.CreateLocationId(cloud, region, zone);

        var options = new ServiceLevelIndicatorOptions
        {
            CustomerResourceId = customerResourceId,
            LocationId = locationId,
            Meter = _meter
        };
        var serviceLevelIndicator = new ServiceLevelIndicator(Options.Create(options));

        var operation = "TestOperation";
        var elapsedTime = 100;
        var attributes = new KeyValuePair<string, object?>[]
        {
            new("Attribute1", "Value1"),
            new("Attribute2", "Value2")
        };

        // Act
        serviceLevelIndicator.RecordLatency(operation, elapsedTime, attributes);

        // Assert
        _expectedTags = new KeyValuePair<string, object?>[]
        {
            new KeyValuePair<string, object?> ( "CustomerResourceId", customerResourceId ),
            new KeyValuePair<string, object?> ( "LocationId", locationId ),
            new KeyValuePair<string, object?> ( "Operation", operation ),
            new KeyValuePair<string, object?> ( "Attribute1", "Value1" ),
            new KeyValuePair<string, object?> ( "Attribute2", "Value2" )
        };

        ValidateMetrics(elapsedTime);
    }

    [Fact]
    public void Customize_instrument_name()
    {
        // Arrange
        var serviceId = Guid.NewGuid();
        var cloud = "public";
        var region = "EastUS";
        var zone = "Zone1";
        var customerResourceId = "TestResourceId";
        var locationId = ServiceLevelIndicator.CreateLocationId(cloud, region, zone);

        const string InstrumentName = "CustomInstrumentName";
        var options = new ServiceLevelIndicatorOptions
        {
            CustomerResourceId = customerResourceId,
            LocationId = locationId,
            Meter = _meter,
            InstrumentName = InstrumentName
        };
        var serviceLevelIndicator = new ServiceLevelIndicator(Options.Create(options));

        var operation = "TestOperation";
        var elapsedTime = 100;
        var attributes = new KeyValuePair<string, object?>[]
        {
            new("Attribute1", "Value1"),
            new("Attribute2", "Value2")
        };

        // Act
        serviceLevelIndicator.RecordLatency(operation, elapsedTime, attributes);

        // Assert
        _expectedTags = new KeyValuePair<string, object?>[]
        {
            new KeyValuePair<string, object?> ( "CustomerResourceId", customerResourceId ),
            new KeyValuePair<string, object?> ( "LocationId", locationId ),
            new KeyValuePair<string, object?> ( "Operation", operation ),
            new KeyValuePair<string, object?> ( "Attribute1", "Value1" ),
            new KeyValuePair<string, object?> ( "Attribute2", "Value2" )
        };

        ValidateMetrics(elapsedTime, InstrumentName);
    }

    private void ValidateMetrics(long elapsedTime, string instrumentName = "LatencySLI")
    {
        _callbackCalled.Should().BeTrue();
        _actualTags.Should().BeEquivalentTo(_expectedTags);
        _instrument!.Name.Should().Be(instrumentName);
        _instrument.Unit.Should().Be("ms");
        _measurement.Should().Be(elapsedTime);
    }

    private void OnMeasurementRecorded(Instrument instrument, long measurement, ReadOnlySpan<KeyValuePair<string, object?>> tags, object? state)
    {
        _actualTags = tags.ToArray();
        _callbackCalled = true;

        _output.WriteLine($"Measurement {measurement}");
        _instrument = instrument;
        _measurement = measurement;
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
