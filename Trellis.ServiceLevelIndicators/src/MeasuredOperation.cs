namespace Trellis.ServiceLevelIndicators;

using System;
using System.Collections.Generic;
using System.Diagnostics;

/// <summary>
/// Represents an in-flight measured operation. Disposing this instance stops the stopwatch
/// and records the elapsed time as a metric.
/// </summary>
public class MeasuredOperation : IDisposable
{
    private bool _disposed;
    private readonly ServiceLevelIndicator _serviceLevelIndicator;
    private readonly Stopwatch _stopWatch;
    private readonly HashSet<string> _attributeNames;
    private ActivityStatusCode _activityStatusCode = ActivityStatusCode.Unset;
    private readonly object _disposeLock = new();

    public MeasuredOperation(ServiceLevelIndicator serviceLevelIndicator, string operation, params KeyValuePair<string, object?>[] attributes) :
        this(serviceLevelIndicator, operation, serviceLevelIndicator.ServiceLevelIndicatorOptions.CustomerResourceId, attributes)
    { }

    public MeasuredOperation(ServiceLevelIndicator serviceLevelIndicator, string operation, string customerResourceId, params KeyValuePair<string, object?>[] attributes)
    {
        _serviceLevelIndicator = serviceLevelIndicator;
        Operation = operation;
        CustomerResourceId = customerResourceId;
        Attributes = new List<KeyValuePair<string, object?>>(attributes.Length);
        _attributeNames = new HashSet<string>(attributes.Length, StringComparer.Ordinal);
        for (var i = 0; i < attributes.Length; i++)
        {
            _serviceLevelIndicator.ValidateAttributeName(attributes[i].Key);
            ValidateUniqueAttributeName(attributes[i].Key);
            Attributes.Add(attributes[i]);
        }

        _stopWatch = Stopwatch.StartNew();
    }

    /// <summary>
    /// Gets or sets the operation name emitted as a tag.
    /// </summary>
    public string Operation { get; set; }

    /// <summary>
    /// Gets or sets the customer resource identifier emitted as a tag.
    /// </summary>
    public string CustomerResourceId { get; set; }

    /// <summary>
    /// Gets the additional OpenTelemetry attributes emitted with the measurement.
    /// </summary>
    public List<KeyValuePair<string, object?>> Attributes { get; }

    /// <summary>
    /// Sets the <see cref="ActivityStatusCode"/> recorded with the measurement.
    /// </summary>
    /// <param name="activityStatusCode">The activity status code.</param>
    public void SetActivityStatusCode(ActivityStatusCode activityStatusCode) => _activityStatusCode = activityStatusCode;

    /// <summary>
    /// Adds a custom attribute to the measurement.
    /// </summary>
    /// <param name="attribute">The attribute name.</param>
    /// <param name="value">The attribute value.</param>
    public void AddAttribute(string attribute, object? value)
    {
        _serviceLevelIndicator.ValidateAttributeName(attribute);
        ValidateUniqueAttributeName(attribute);
        Attributes.Add(new KeyValuePair<string, object?>(attribute, value));
    }

    private void ValidateUniqueAttributeName(string attribute)
    {
        if (!_attributeNames.Add(attribute))
        {
            throw new ArgumentException(
                $"Service Level Indicator attribute '{attribute}' was added more than once. Metric attribute names must be unique.",
                nameof(attribute));
        }
    }

    protected virtual void Dispose(bool disposing)
    {
        lock (_disposeLock)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    _stopWatch.Stop();
                    var elapsedTime = _stopWatch.ElapsedMilliseconds;
                    Attributes.Add(new KeyValuePair<string, object?>(_serviceLevelIndicator.ServiceLevelIndicatorOptions.ActivityStatusCodeAttributeName, _activityStatusCode.ToString()));
                    _serviceLevelIndicator.RecordMeasurement(Operation, CustomerResourceId, elapsedTime, Attributes.ToArray());
                }

                _disposed = true;
            }
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
}
