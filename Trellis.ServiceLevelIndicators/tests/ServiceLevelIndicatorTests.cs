namespace Trellis.ServiceLevelIndicators.Tests;

using System;
using System.Collections.Generic;
using System.Diagnostics.Metrics;
using System.Threading;
using Microsoft.Extensions.Options;

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

        _actualTags = [];
        _expectedTags = [];
    }

    [Fact]
    public void Record()
    {
        // Arrange
        var customerResourceId = "TestResourceId";
        var locationId = "TestLocationId";

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
        serviceLevelIndicator.Record(operation, elapsedTime, attributes);

        // Assert
        _expectedTags =
        [
            new("CustomerResourceId", customerResourceId),
            new("LocationId", locationId),
            new("Operation", operation),
            new("Attribute1", "Value1"),
            new("Attribute2", "Value2")
        ];

        ValidateMetrics(elapsedTime);
    }

    [Fact]
    public async Task Will_measure_code_block()
    {
        // Arrange
        var customerResourceId = "TestResourceId";
        var locationId = "TestLocationId";
        int sleepTime = 500;

        var options = new ServiceLevelIndicatorOptions
        {
            CustomerResourceId = customerResourceId,
            LocationId = locationId,
            Meter = _meter
        };
        var serviceLevelIndicator = new ServiceLevelIndicator(Options.Create(options));

        // Act
        await MeasureCodeBlock(serviceLevelIndicator);

        // Assert
        _expectedTags =
        [
            new("CustomerResourceId", customerResourceId),
            new("LocationId", locationId),
            new("Operation", "SleepWorker"),
            new("activity.status.code", nameof(System.Diagnostics.ActivityStatusCode.Ok)),
        ];

        ValidateMetrics(sleepTime, approx: 100);

        async Task MeasureCodeBlock(ServiceLevelIndicator serviceLevelIndicator)
        {
            using var measuredOperation = serviceLevelIndicator.StartMeasuring("SleepWorker");
            await Task.Delay(sleepTime);
            measuredOperation.SetActivityStatusCode(System.Diagnostics.ActivityStatusCode.Ok);
        }
    }

    [Fact]
    public void Uses_default_meter_when_none_provided()
    {
        // Arrange
        var options = new ServiceLevelIndicatorOptions
        {
            CustomerResourceId = "TestResourceId",
            LocationId = "TestLocationId"
        };

        // Act
        var serviceLevelIndicator = new ServiceLevelIndicator(Options.Create(options));

        // Assert
        serviceLevelIndicator.ServiceLevelIndicatorOptions.Meter.Should().NotBeNull();
        serviceLevelIndicator.ServiceLevelIndicatorOptions.Meter.Name.Should().Be(ServiceLevelIndicator.DefaultMeterName);
    }

    [Fact]
    public void Record_with_no_attributes()
    {
        // Arrange
        var customerResourceId = "TestResourceId";
        var locationId = "TestLocationId";

        var options = new ServiceLevelIndicatorOptions
        {
            CustomerResourceId = customerResourceId,
            LocationId = locationId,
            Meter = _meter
        };
        var serviceLevelIndicator = new ServiceLevelIndicator(Options.Create(options));

        var operation = "TestOperation";
        var elapsedTime = 50;

        // Act
        serviceLevelIndicator.Record(operation, elapsedTime);

        // Assert
        _expectedTags =
        [
            new("CustomerResourceId", customerResourceId),
            new("LocationId", locationId),
            new("Operation", operation)
        ];

        ValidateMetrics(elapsedTime);
    }

    [Fact]
    public void Record_with_customerResourceId_override()
    {
        // Arrange
        var defaultCustomerResourceId = "DefaultResourceId";
        var overrideCustomerResourceId = "OverrideResourceId";
        var locationId = "TestLocationId";

        var options = new ServiceLevelIndicatorOptions
        {
            CustomerResourceId = defaultCustomerResourceId,
            LocationId = locationId,
            Meter = _meter
        };
        var serviceLevelIndicator = new ServiceLevelIndicator(Options.Create(options));

        var operation = "TestOperation";
        var elapsedTime = 75;

        // Act
        serviceLevelIndicator.Record(operation, overrideCustomerResourceId, elapsedTime);

        // Assert
        _expectedTags =
        [
            new("CustomerResourceId", overrideCustomerResourceId),
            new("LocationId", locationId),
            new("Operation", operation)
        ];

        ValidateMetrics(elapsedTime);
    }

    [Fact]
    public async Task MeasuredOperation_double_dispose_does_not_record_twice()
    {
        // Arrange
        var customerResourceId = "TestResourceId";
        var locationId = "TestLocationId";
        int callCount = 0;

        var options = new ServiceLevelIndicatorOptions
        {
            CustomerResourceId = customerResourceId,
            LocationId = locationId,
            Meter = _meter
        };
        var serviceLevelIndicator = new ServiceLevelIndicator(Options.Create(options));

        _meterListener.SetMeasurementEventCallback<long>((Instrument instrument, long measurement, ReadOnlySpan<KeyValuePair<string, object?>> tags, object? state) =>
        {
            Interlocked.Increment(ref callCount);
            OnMeasurementRecorded(instrument, measurement, tags, state);
        });

        // Act
        var measuredOperation = serviceLevelIndicator.StartMeasuring("DoubleDispose");
        await Task.Delay(50, TestContext.Current.CancellationToken);
        measuredOperation.SetActivityStatusCode(System.Diagnostics.ActivityStatusCode.Ok);
        measuredOperation.Dispose();
        measuredOperation.Dispose(); // Second dispose should be a no-op

        // Assert
        callCount.Should().Be(1);
    }

    [Fact]
    public void Customize_instrument_name()
    {
        // Arrange
        const string InstrumentName = "CustomInstrumentName";
        var customerResourceId = "TestResourceId";
        var locationId = "TestLocationId";

        var options = new ServiceLevelIndicatorOptions
        {
            CustomerResourceId = customerResourceId,
            LocationId = locationId,
            Meter = _meter,
            DurationInstrumentName = InstrumentName
        };
        var serviceLevelIndicator = new ServiceLevelIndicator(Options.Create(options));

        var operation = "TestOperation";
        var elapsedTime = 30;

        // Act
        serviceLevelIndicator.Record(operation, elapsedTime);

        // Assert
        _expectedTags =
        [
            new("CustomerResourceId", customerResourceId),
            new("LocationId", locationId),
            new("Operation", operation)
        ];

        ValidateMetrics(elapsedTime, InstrumentName);
    }

    [Fact]
    public void Dispose_when_meter_was_internally_created_disposes_the_meter()
    {
        // Arrange
        var options = new ServiceLevelIndicatorOptions
        {
            CustomerResourceId = "TestResourceId",
            LocationId = "TestLocationId"
        };
        var sli = new ServiceLevelIndicator(Options.Create(options));
        var internalMeter = sli.ServiceLevelIndicatorOptions.Meter;
        internalMeter.Should().NotBeNull();

        var measurementsCompleted = false;
        using var listener = new MeterListener
        {
            InstrumentPublished = (instrument, l) =>
            {
                if (ReferenceEquals(instrument.Meter, internalMeter))
                    l.EnableMeasurementEvents(instrument);
            },
            MeasurementsCompleted = (instrument, _) =>
            {
                if (ReferenceEquals(instrument.Meter, internalMeter))
                    measurementsCompleted = true;
            }
        };
        listener.Start();

        // Act
        sli.Dispose();

        // Assert: MeasurementsCompleted fires when the instrument's parent Meter is disposed.
        measurementsCompleted.Should().BeTrue();
    }

    [Fact]
    public void Dispose_when_meter_was_supplied_does_not_dispose_user_meter()
    {
        // Arrange
        using var userMeter = new Meter("UserOwnedMeter", "1.0.0");
        var options = new ServiceLevelIndicatorOptions
        {
            CustomerResourceId = "TestResourceId",
            LocationId = "TestLocationId",
            Meter = userMeter
        };
        var sli = new ServiceLevelIndicator(Options.Create(options));

        var measurementsCompleted = false;
        using var listener = new MeterListener
        {
            InstrumentPublished = (instrument, l) =>
            {
                if (ReferenceEquals(instrument.Meter, userMeter))
                    l.EnableMeasurementEvents(instrument);
            },
            MeasurementsCompleted = (instrument, _) =>
            {
                if (ReferenceEquals(instrument.Meter, userMeter))
                    measurementsCompleted = true;
            }
        };
        listener.Start();

        // Act
        sli.Dispose();

        // Assert: user-owned meter is NOT disposed by SLI; user is responsible for its lifetime.
        measurementsCompleted.Should().BeFalse();
    }

    [Fact]
    public void Disposing_twice_is_safe()
    {
        // Arrange
        var options = new ServiceLevelIndicatorOptions
        {
            CustomerResourceId = "TestResourceId",
            LocationId = "TestLocationId"
        };
        var sli = new ServiceLevelIndicator(Options.Create(options));

        // Act / Assert
        Action disposeTwice = () => { sli.Dispose(); sli.Dispose(); };
        disposeTwice.Should().NotThrow();
    }

    private void ValidateMetrics(int elapsedTime, string instrumentName = "operation.duration", int? approx = null)
    {
        _callbackCalled.Should().BeTrue();
        _actualTags.Should().BeEquivalentTo(_expectedTags);
        _instrument!.Name.Should().Be(instrumentName);
        _instrument.Unit.Should().Be("ms");

        if (approx.HasValue)
            _measurement.Should().BeInRange(elapsedTime - approx.Value, elapsedTime + approx.Value);
        else
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