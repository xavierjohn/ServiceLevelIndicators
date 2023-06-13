namespace ServiceLevelIndicators;

using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Metrics;

public class ServiceLevelIndicator
{
    public string CustomerResourceId { get; }
    public string LocationId { get; }

    private readonly Histogram<long> _responseLatencyHistogram;

    public ServiceLevelIndicator(string customerResourceId, string locationId, Meter meter, string name = "LatencySLI")
    {
        CustomerResourceId = customerResourceId;
        LocationId = locationId;
        _responseLatencyHistogram = meter.CreateHistogram<long>(name);
    }

    public void RecordLatency(string operation, long elapsedTime, params KeyValuePair<string, object?>[] tags)
    {
        var tagList = new TagList
        {
            { "CustomerResourceId", CustomerResourceId },
            { "LocationId", LocationId },
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
