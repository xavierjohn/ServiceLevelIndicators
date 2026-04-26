namespace Trellis.ServiceLevelIndicators.Tests;

using System;
using System.Collections.Generic;
using System.Diagnostics.Metrics;
using System.Threading;
using Microsoft.Extensions.DependencyInjection;
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
        const string MeterName = "Trellis.SLI";
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

    [Fact]
    public void AddServiceLevelIndicator_registers_core_services_without_asp_package()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        var builder = services.AddServiceLevelIndicator(options =>
        {
            options.LocationId = "TestLocationId";
            options.CustomerResourceId = "TestResourceId";
        });

        // Assert
        builder.Services.Should().BeSameAs(services);
        services.Should().Contain(descriptor =>
            descriptor.ServiceType == typeof(ServiceLevelIndicator) &&
            descriptor.Lifetime == ServiceLifetime.Singleton);
        services.Should().Contain(descriptor =>
            descriptor.ServiceType == typeof(IConfigureOptions<ServiceLevelIndicatorOptions>));
    }

    [Theory]
    [InlineData("CustomerResourceId")]
    [InlineData("LocationId")]
    [InlineData("Operation")]
    [InlineData("activity.status.code")]
    public void Record_rejects_reserved_custom_attribute_names(string reservedName)
    {
        // Arrange
        var serviceLevelIndicator = CreateServiceLevelIndicator();

        // Act
        Action act = () => serviceLevelIndicator.Record(
            "TestOperation",
            elapsedTime: 1,
            new KeyValuePair<string, object?>(reservedName, "override"));

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*reserved Service Level Indicator attribute name*");
    }

    [Theory]
    [InlineData("CustomerResourceId")]
    [InlineData("LocationId")]
    [InlineData("Operation")]
    [InlineData("activity.status.code")]
    public void StartMeasuring_rejects_reserved_initial_attribute_names(string reservedName)
    {
        // Arrange
        var serviceLevelIndicator = CreateServiceLevelIndicator();

        // Act
        Action act = () => serviceLevelIndicator.StartMeasuring(
            "TestOperation",
            new KeyValuePair<string, object?>(reservedName, "override"));

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*reserved Service Level Indicator attribute name*");
    }

    [Theory]
    [InlineData("CustomerResourceId")]
    [InlineData("LocationId")]
    [InlineData("Operation")]
    [InlineData("activity.status.code")]
    public void MeasuredOperation_AddAttribute_rejects_reserved_attribute_names(string reservedName)
    {
        // Arrange
        var serviceLevelIndicator = CreateServiceLevelIndicator();
        using var measuredOperation = serviceLevelIndicator.StartMeasuring("TestOperation");

        // Act
        Action act = () => measuredOperation.AddAttribute(reservedName, "override");

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*reserved Service Level Indicator attribute name*");
    }

    [Fact]
    public void MeasuredOperation_AddAttribute_rejects_blank_attribute_name_with_parameter_name()
    {
        // Arrange
        var serviceLevelIndicator = CreateServiceLevelIndicator();
        using var measuredOperation = serviceLevelIndicator.StartMeasuring("TestOperation");

        // Act
        Action act = () => measuredOperation.AddAttribute(" ", "override");

        // Assert
        act.Should().Throw<ArgumentException>()
            .Where(ex => ex.ParamName == "attribute");
    }

    [Fact]
    public void StartMeasuring_rejects_duplicate_initial_attribute_names()
    {
        // Arrange
        var serviceLevelIndicator = CreateServiceLevelIndicator();

        // Act
        Action act = () => serviceLevelIndicator.StartMeasuring(
            "TestOperation",
            new KeyValuePair<string, object?>("CustomAttribute", "first"),
            new KeyValuePair<string, object?>("CustomAttribute", "second"));

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*added more than once*");
    }

    [Fact]
    public void MeasuredOperation_AddAttribute_rejects_duplicate_attribute_names()
    {
        // Arrange
        var serviceLevelIndicator = CreateServiceLevelIndicator();
        using var measuredOperation = serviceLevelIndicator.StartMeasuring("TestOperation");
        measuredOperation.AddAttribute("CustomAttribute", "first");

        // Act
        Action act = () => measuredOperation.AddAttribute("CustomAttribute", "second");

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*added more than once*");
    }

    [Fact]
    public void Constructor_rejects_activity_status_attribute_name_that_collides_with_core_tags()
    {
        // Arrange
        var options = new ServiceLevelIndicatorOptions
        {
            CustomerResourceId = "TestResourceId",
            LocationId = "TestLocationId",
            Meter = _meter,
            ActivityStatusCodeAttributeName = "Operation"
        };

        // Act
        Action act = () =>
        {
            using var serviceLevelIndicator = new ServiceLevelIndicator(Options.Create(options));
        };

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*reserved Service Level Indicator attribute name*");
    }

    [Fact]
    public void MeasuredOperation_Dispose_rejects_duplicate_attribute_names_added_by_direct_list_mutation()
    {
        // Arrange
        var serviceLevelIndicator = CreateServiceLevelIndicator();
        var measuredOperation = serviceLevelIndicator.StartMeasuring("TestOperation");
        measuredOperation.Attributes.Add(new KeyValuePair<string, object?>("CustomAttribute", "first"));
        measuredOperation.Attributes.Add(new KeyValuePair<string, object?>("CustomAttribute", "second"));

        // Act
        Action act = measuredOperation.Dispose;

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*added more than once*");
    }

    [Fact]
    public void MeasuredOperation_Dispose_rejects_blank_attribute_names_added_by_direct_list_mutation()
    {
        // Arrange
        var serviceLevelIndicator = CreateServiceLevelIndicator();
        var measuredOperation = serviceLevelIndicator.StartMeasuring("TestOperation");
        measuredOperation.Attributes.Add(new KeyValuePair<string, object?>(" ", "invalid"));

        // Act
        Action act = measuredOperation.Dispose;

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*attribute names cannot be null, empty, or whitespace*");
    }

    private ServiceLevelIndicator CreateServiceLevelIndicator() =>
        new(Options.Create(new ServiceLevelIndicatorOptions
        {
            CustomerResourceId = "TestResourceId",
            LocationId = "TestLocationId",
            Meter = _meter
        }));

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
