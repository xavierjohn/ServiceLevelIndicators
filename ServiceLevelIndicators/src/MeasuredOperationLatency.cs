namespace ServiceLevelIndicators;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;

public class MeasuredOperationLatency : IDisposable
{
    private bool _disposed;
    private readonly ServiceLevelIndicator _serviceLevelIndicator;
    private readonly Stopwatch _stopWatch;
    private ActivityStatusCode _activityStatusCode = ActivityStatusCode.Unset;
    private int _httpStatusCode;
    private readonly object _disposeLock = new();

    public MeasuredOperationLatency(ServiceLevelIndicator serviceLevelIndicator, string operation, params KeyValuePair<string, object?>[] attributes) :
        this(serviceLevelIndicator, operation, serviceLevelIndicator.ServiceLevelIndicatorOptions.CustomerResourceId, attributes)
    { }

    public MeasuredOperationLatency(ServiceLevelIndicator serviceLevelIndicator, string operation, string customerResourceId, params KeyValuePair<string, object?>[] attributes)
    {
        _serviceLevelIndicator = serviceLevelIndicator;
        Operation = operation;
        CustomerResourceId = customerResourceId;
        Attributes = attributes.ToList();
        _stopWatch = Stopwatch.StartNew();
    }

    public string Operation { get; set; }
    public string CustomerResourceId { get; set; }

    // OTEL Attributes to emit
    public List<KeyValuePair<string, object?>> Attributes { get; }

    public void SetState(ActivityStatusCode activityStatusCode) => _activityStatusCode = activityStatusCode;
    public void SetState(HttpStatusCode httpStatusCode) => _httpStatusCode = (int)httpStatusCode;
    public void SetHttpStatusCode(int httpStatusCode) => _httpStatusCode = httpStatusCode;

    public void SetApiVersion(string apiVersion) => AddAttribute(_serviceLevelIndicator.ServiceLevelIndicatorOptions.ApiVersionAttributeName, apiVersion);

    public void AddAttribute(string attribute, object? value) => Attributes.Add(new KeyValuePair<string, object?>(attribute, value));

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
                    Attributes.Add(new KeyValuePair<string, object?>("Status", _activityStatusCode.ToString()));
                    if (_httpStatusCode > 0)
                        Attributes.Add(new KeyValuePair<string, object?>("HttpStatusCode", _httpStatusCode));
                    _serviceLevelIndicator.RecordLatency(Operation, CustomerResourceId, elapsedTime, Attributes.ToArray());
                }

                _disposed = true;
            }
        }
    }

    public void Dispose()
    {
        // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
}
