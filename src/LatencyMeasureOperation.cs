﻿namespace Asp.ServiceLevelIndicators;

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
    private readonly KeyValuePair<string, object?>[] _tags;
    private readonly Stopwatch _stopWatch;
    private ActivityStatusCode _activityStatusCode = ActivityStatusCode.Unset;
    private int _httpStatusCode;

    public LatencyMeasureOperation(ServiceLevelIndicator serviceLevelIndicator, string operation, params KeyValuePair<string, object?>[] tags) :
        this(serviceLevelIndicator, operation, serviceLevelIndicator.ServiceLevelIndicatorOptions.CustomerResourceId, tags)
    { }

    public LatencyMeasureOperation(ServiceLevelIndicator serviceLevelIndicator, string operation, string customerResourceId, params KeyValuePair<string, object?>[] tags)
    {
        _serviceLevelIndicator = serviceLevelIndicator;
        _operation = operation;
        _customerResourceId = customerResourceId;
        _tags = tags;
        _stopWatch = new Stopwatch();
        _stopWatch.Start();
    }

    public void SetState(ActivityStatusCode activityStatusCode) => _activityStatusCode = activityStatusCode;
    public void SetState(HttpStatusCode httpStatusCode) => _httpStatusCode = (int)httpStatusCode;
    public void SetHttpStatusCode(int httpStatusCode) => _httpStatusCode = httpStatusCode;

    public void SetCustomerResourceId(string customerResourceId) => _customerResourceId = customerResourceId;

    internal bool DoEmitMetrics { get; set; } = true;

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposedValue)
        {
            if (disposing && DoEmitMetrics)
            {
                _stopWatch.Stop();
                var elapsedTime = _stopWatch.ElapsedMilliseconds;
                var tags = _tags.ToList();
                tags.Add(new KeyValuePair<string, object?>("Status", _activityStatusCode.ToString()));
                if (_httpStatusCode > 0)
                    tags.Add(new KeyValuePair<string, object?>("HttpStatusCode", _httpStatusCode));
                _serviceLevelIndicator.RecordLatency(_operation, _customerResourceId, elapsedTime, tags.ToArray());
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
