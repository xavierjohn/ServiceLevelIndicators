namespace Asp.ServiceLevelIndicators;

using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using Microsoft.Extensions.Options;

public class ServiceLevelIndicator
{
    public ServiceLevelIndicatorOptions ServiceLevelIndicatorOptions { get; }

    private readonly Histogram<long> _responseLatencyHistogram;

    public ServiceLevelIndicator(IOptions<ServiceLevelIndicatorOptions> options, IServiceLevelIndicatorMeter meter)
    {
        ServiceLevelIndicatorOptions = options.Value;
        _responseLatencyHistogram = meter.Meter.CreateHistogram<long>(ServiceLevelIndicatorOptions.InstrumentName);
    }

    public void RecordLatency(string operation, long elapsedTime, params KeyValuePair<string, object?>[] tags) =>
        RecordLatency(operation, ServiceLevelIndicatorOptions.DefaultCustomerResourceId, elapsedTime, tags);

    public void RecordLatency(string operation, string customerResourseId, long elapsedTime, params KeyValuePair<string, object?>[] tags)
    {
        var tagList = new TagList
        {
            { "CustomerResourceId", customerResourseId },
            { "LocationId", ServiceLevelIndicatorOptions.LocationId },
            { "Operation", operation }
        };

        for (var i = 0; i < tags.Length; i++)
            tagList.Add(tags[i]);

        _responseLatencyHistogram.Record(elapsedTime, tagList);
    }

    public LatencyMeasureOperation StartLatencyMeasureOperation(string operation, params KeyValuePair<string, object?>[] tags) => new(this, operation, tags);

    public static string CreateCustomerResourceId(string product, string serviceName) => product + "_" + serviceName;

    public static string CreateLocationId(string cloud, string region, string? stamp = null)
    {
        var id = cloud + "_" + region;
        if (!string.IsNullOrWhiteSpace(stamp)) id += "_" + stamp;
        return id;
    }
}
