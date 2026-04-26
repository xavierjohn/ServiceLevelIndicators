namespace Trellis.ServiceLevelIndicators;

using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Reflection;
using Microsoft.Extensions.Options;

/// <summary>
/// Core service that creates and records Service Level Indicator metrics
/// using an OpenTelemetry <see cref="Histogram{T}"/>.
/// </summary>
/// <remarks>
/// Registered as a singleton. The container disposes this instance at host shutdown.
/// When no <see cref="Meter"/> is supplied via <see cref="ServiceLevelIndicatorOptions.Meter"/>,
/// this class creates one internally and disposes it on shutdown. A user-supplied
/// <see cref="Meter"/> is never disposed by this class; the caller owns its lifetime.
/// </remarks>
public sealed class ServiceLevelIndicator : IDisposable
{
    /// <summary>
    /// Default meter name used when no <see cref="Meter"/> is provided in options.
    /// </summary>
    public const string DefaultMeterName = "Trellis.SLI";

    /// <summary>
    /// Gets the options used to configure this instance.
    /// </summary>
    public ServiceLevelIndicatorOptions ServiceLevelIndicatorOptions { get; }

    private readonly Histogram<long> _responseLatencyHistogram;
    private readonly Meter _meter;
    private readonly bool _ownsMeter;
    private bool _disposed;

    public ServiceLevelIndicator(IOptions<ServiceLevelIndicatorOptions> options)
    {
        ServiceLevelIndicatorOptions = options.Value;

        ArgumentException.ThrowIfNullOrWhiteSpace(ServiceLevelIndicatorOptions.LocationId, nameof(ServiceLevelIndicatorOptions.LocationId));
        ArgumentException.ThrowIfNullOrWhiteSpace(ServiceLevelIndicatorOptions.DurationInstrumentName, nameof(ServiceLevelIndicatorOptions.DurationInstrumentName));
        ValidateActivityStatusCodeAttributeName();

        if (ServiceLevelIndicatorOptions.Meter == null)
        {
            AssemblyName AssemblyName = typeof(ServiceLevelIndicator).Assembly.GetName();
            string InstrumentationVersion = AssemblyName.Version!.ToString();
            _meter = new Meter(DefaultMeterName, InstrumentationVersion);
            ServiceLevelIndicatorOptions.Meter = _meter;
            _ownsMeter = true;
        }
        else
        {
            _meter = ServiceLevelIndicatorOptions.Meter;
            _ownsMeter = false;
        }

        _responseLatencyHistogram = _meter.CreateHistogram<long>(ServiceLevelIndicatorOptions.DurationInstrumentName, "ms", "Duration of the operation.");
    }

    /// <summary>
    /// Disposes the internally-created <see cref="Meter"/> if this instance created it.
    /// User-supplied meters are never disposed by this class.
    /// </summary>
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        if (_ownsMeter)
            _meter.Dispose();
    }

    /// <summary>
    /// Records an operation measurement using the default <see cref="ServiceLevelIndicatorOptions.CustomerResourceId"/>.
    /// </summary>
    /// <param name="operation">The operation name.</param>
    /// <param name="elapsedTime">Elapsed time in milliseconds.</param>
    /// <param name="attributes">Additional measurement attributes.</param>
    public void Record(string operation, long elapsedTime, params KeyValuePair<string, object?>[] attributes) =>
        Record(operation, ServiceLevelIndicatorOptions.CustomerResourceId, elapsedTime, attributes);

    /// <summary>
    /// Records an operation measurement with an explicit customer resource identifier.
    /// </summary>
    /// <param name="operation">The operation name.</param>
    /// <param name="customerResourceId">The customer resource identifier.</param>
    /// <param name="elapsedTime">Elapsed time in milliseconds.</param>
    /// <param name="attributes">Additional measurement attributes.</param>
    public void Record(string operation, string customerResourceId, long elapsedTime, params KeyValuePair<string, object?>[] attributes)
    {
        ValidateAttributes(attributes);
        RecordMeasurement(operation, customerResourceId, elapsedTime, attributes);
    }

    internal void RecordMeasurement(string operation, string customerResourceId, long elapsedTime, params KeyValuePair<string, object?>[] attributes)
    {
        ValidateRecordAttributeNames(attributes);

        var tagList = new TagList
        {
            { "CustomerResourceId", customerResourceId },
            { "LocationId", ServiceLevelIndicatorOptions.LocationId },
            { "Operation", operation }
        };

        for (var i = 0; i < attributes.Length; i++)
            tagList.Add(attributes[i]);

        _responseLatencyHistogram.Record(elapsedTime, tagList);
    }

    /// <summary>
    /// Starts measuring an operation. Dispose the returned <see cref="MeasuredOperation"/> to record the elapsed time.
    /// </summary>
    /// <param name="operation">The operation name.</param>
    /// <param name="attributes">Additional measurement attributes.</param>
    /// <returns>A <see cref="MeasuredOperation"/> that records the metric on disposal.</returns>
    public MeasuredOperation StartMeasuring(string operation, params KeyValuePair<string, object?>[] attributes) => new(this, operation, attributes);

    internal void ValidateAttributeName(string attribute)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(attribute, nameof(attribute));

        if (attribute is "CustomerResourceId" or "LocationId" or "Operation" ||
            attribute == ServiceLevelIndicatorOptions.ActivityStatusCodeAttributeName)
        {
            throw new ArgumentException(
                $"'{attribute}' is a reserved Service Level Indicator attribute name and cannot be used as a custom metric attribute.",
                nameof(attribute));
        }
    }

    private void ValidateActivityStatusCodeAttributeName()
    {
        var attribute = ServiceLevelIndicatorOptions.ActivityStatusCodeAttributeName;
        ArgumentException.ThrowIfNullOrWhiteSpace(attribute, nameof(ServiceLevelIndicatorOptions.ActivityStatusCodeAttributeName));

        if (attribute is "CustomerResourceId" or "LocationId" or "Operation")
        {
            throw new ArgumentException(
                $"'{attribute}' is a reserved Service Level Indicator attribute name and cannot be used as the activity status code attribute name.",
                nameof(ServiceLevelIndicatorOptions.ActivityStatusCodeAttributeName));
        }
    }

    private void ValidateAttributes(ReadOnlySpan<KeyValuePair<string, object?>> attributes)
    {
        for (var i = 0; i < attributes.Length; i++)
            ValidateAttributeName(attributes[i].Key);
    }

    private static void ValidateRecordAttributeNames(ReadOnlySpan<KeyValuePair<string, object?>> attributes)
    {
        HashSet<string>? names = null;

        for (var i = 0; i < attributes.Length; i++)
        {
            if (string.IsNullOrWhiteSpace(attributes[i].Key))
            {
                throw new ArgumentException(
                    "Service Level Indicator attribute names cannot be null, empty, or whitespace.",
                    nameof(attributes));
            }

            names ??= new HashSet<string>(attributes.Length + 3, StringComparer.Ordinal)
            {
                "CustomerResourceId",
                "LocationId",
                "Operation"
            };

            if (!names.Add(attributes[i].Key))
            {
                throw new InvalidOperationException(
                    $"Service Level Indicator attribute '{attributes[i].Key}' was added more than once. Metric attribute names must be unique.");
            }
        }
    }

    /// <summary>
    /// Creates a customer resource identifier from a Service Tree GUID.
    /// </summary>
    /// <param name="serviceId">A non-empty Service Tree identifier.</param>
    /// <returns>A formatted <c>ServiceTreeId://</c> URI string.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="serviceId"/> is <see cref="Guid.Empty"/>.</exception>
    public static string CreateCustomerResourceId(Guid serviceId)
    {
        if (serviceId == Guid.Empty) throw new ArgumentNullException(nameof(serviceId));
        return "ServiceTreeId://" + serviceId.ToString();
    }

    /// <summary>
    /// Creates a location identifier in the <c>ms-loc://az</c> format.
    /// </summary>
    /// <param name="cloud">Cloud name (e.g., "public").</param>
    /// <param name="region">Optional region (e.g., "West US 3").</param>
    /// <param name="zone">Optional availability zone.</param>
    /// <returns>A formatted location identifier.</returns>
    public static string CreateLocationId(string cloud, string? region = null, string? zone = null)
    {
        var arr = new string?[] { "ms-loc://az", cloud, region, zone };
        var id = string.Join("/", arr.Where(s => !string.IsNullOrEmpty(s)));
        return id;
    }
}
