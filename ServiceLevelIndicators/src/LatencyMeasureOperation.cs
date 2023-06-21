namespace ServiceLevelIndicators;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;

public class LatencyMeasureOperation : IDisposable
{
    private bool _disposedValue;
    private readonly ServiceLevelIndicator _serviceLevelIndicator;
    private readonly string _operation;
    private string _customerResourceId;
    private readonly Stopwatch _stopWatch;
    private ActivityStatusCode _activityStatusCode = ActivityStatusCode.Unset;
    private int _httpStatusCode;

    public LatencyMeasureOperation(ServiceLevelIndicator serviceLevelIndicator, string operation, params KeyValuePair<string, object?>[] attributes) :
        this(serviceLevelIndicator, operation, serviceLevelIndicator.ServiceLevelIndicatorOptions.CustomerResourceId, attributes)
    { }

    public LatencyMeasureOperation(ServiceLevelIndicator serviceLevelIndicator, string operation, string customerResourceId, params KeyValuePair<string, object?>[] attributes)
    {
        _serviceLevelIndicator = serviceLevelIndicator;
        _operation = operation;
        _customerResourceId = customerResourceId;
        Attributes = attributes.ToList();
        _stopWatch = new Stopwatch();
        _stopWatch.Start();
    }

    // OTEL Attributes to emit
    public List<KeyValuePair<string, object?>> Attributes { get; }

    public void SetState(ActivityStatusCode activityStatusCode) => _activityStatusCode = activityStatusCode;
    public void SetState(HttpStatusCode httpStatusCode) => _httpStatusCode = (int)httpStatusCode;
    public void SetHttpStatusCode(int httpStatusCode) => _httpStatusCode = httpStatusCode;

    public void SetCustomerResourceId(string customerResourceId) => _customerResourceId = customerResourceId;

    public void SetApiVersion(string apiVersion) => AddAttribute("api_version", apiVersion);

    public void AddAttribute(string attribute, object? value) => Attributes.Add(new KeyValuePair<string, object?>(attribute, value));

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposedValue)
        {
            if (disposing)
            {
                _stopWatch.Stop();
                var elapsedTime = _stopWatch.ElapsedMilliseconds;
                Attributes.Add(new KeyValuePair<string, object?>("Status", _activityStatusCode.ToString()));
                if (_httpStatusCode > 0)
                    Attributes.Add(new KeyValuePair<string, object?>("HttpStatusCode", _httpStatusCode));
                _serviceLevelIndicator.RecordLatency(_operation, _customerResourceId, elapsedTime, Attributes.ToArray());
            }

            _disposedValue = true;
        }
    }

    public void Dispose()
    {
        // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
}
