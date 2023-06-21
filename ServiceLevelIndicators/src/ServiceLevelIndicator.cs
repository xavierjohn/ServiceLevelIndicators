namespace ServiceLevelIndicators;

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

    public void RecordLatency(string operation, long elapsedTime, params KeyValuePair<string, object?>[] attributes) =>
        RecordLatency(operation, ServiceLevelIndicatorOptions.CustomerResourceId, elapsedTime, attributes);

    public void RecordLatency(string operation, string customerResourseId, long elapsedTime, params KeyValuePair<string, object?>[] attributes)
    {
        var tagList = new TagList
        {
            { "CustomerResourceId", customerResourseId },
            { "LocationId", ServiceLevelIndicatorOptions.LocationId },
            { "Operation", operation }
        };

        for (var i = 0; i < attributes.Length; i++)
            tagList.Add(attributes[i]);

        _responseLatencyHistogram.Record(elapsedTime, tagList);
    }

    public LatencyMeasureOperation StartLatencyMeasureOperation(string operation, params KeyValuePair<string, object?>[] attributes) => new(this, operation, attributes);

    public static string CreateCustomerResourceId(Guid serviceId)
    {
        if (serviceId == Guid.Empty) throw new ArgumentNullException(nameof(serviceId));
        return "ServiceTreeId://" + serviceId.ToString();
    }

    public static string CreateLocationId(string cloud, string? region = null, string? zone = null)
    {
        var arr = new string?[] { "ms-loc://az", cloud, region, zone };
        var id = string.Join("/", arr.Where(s => !string.IsNullOrEmpty(s)));
        return id;
    }
}
