namespace ServiceLevelIndicators;

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
/// Registered as a singleton. If the underlying <see cref="Meter"/> is disposed externally,
/// recording becomes a silent no-op per OpenTelemetry convention.
/// </remarks>
public class ServiceLevelIndicator
{
    /// <summary>
    /// Default meter name used when no <see cref="Meter"/> is provided in options.
    /// </summary>
    public const string DefaultMeterName = nameof(ServiceLevelIndicator);

    /// <summary>
    /// Gets the options used to configure this instance.
    /// </summary>
    public ServiceLevelIndicatorOptions ServiceLevelIndicatorOptions { get; }

    private readonly Histogram<long> _responseLatencyHistogram;

    public ServiceLevelIndicator(IOptions<ServiceLevelIndicatorOptions> options)
    {
        ServiceLevelIndicatorOptions = options.Value;

        ArgumentException.ThrowIfNullOrWhiteSpace(ServiceLevelIndicatorOptions.LocationId, nameof(ServiceLevelIndicatorOptions.LocationId));
        ArgumentException.ThrowIfNullOrWhiteSpace(ServiceLevelIndicatorOptions.DurationInstrumentName, nameof(ServiceLevelIndicatorOptions.DurationInstrumentName));

        if (ServiceLevelIndicatorOptions.Meter == null)
        {
            AssemblyName AssemblyName = typeof(ServiceLevelIndicator).Assembly.GetName();
            string InstrumentationVersion = AssemblyName.Version!.ToString();
            ServiceLevelIndicatorOptions.Meter = new(DefaultMeterName, InstrumentationVersion);
        }

        _responseLatencyHistogram = ServiceLevelIndicatorOptions.Meter.CreateHistogram<long>(ServiceLevelIndicatorOptions.DurationInstrumentName, "ms", "Duration of the operation.");
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