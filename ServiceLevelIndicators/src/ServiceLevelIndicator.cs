namespace ServiceLevelIndicators;

using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Reflection;
using Microsoft.Extensions.Options;

public class ServiceLevelIndicator
{
    public const string DefaultMeterName = nameof(ServiceLevelIndicator);
    public ServiceLevelIndicatorOptions ServiceLevelIndicatorOptions { get; }

    private readonly Histogram<long> _responseLatencyHistogram;

    public ServiceLevelIndicator(IOptions<ServiceLevelIndicatorOptions> options)
    {
        ServiceLevelIndicatorOptions = options.Value;
        if (ServiceLevelIndicatorOptions.Meter == null)
        {
            AssemblyName AssemblyName = typeof(ServiceLevelIndicator).Assembly.GetName();
            string InstrumentationVersion = AssemblyName.Version!.ToString();
            ServiceLevelIndicatorOptions.Meter = new(DefaultMeterName, InstrumentationVersion);
        }

        _responseLatencyHistogram = ServiceLevelIndicatorOptions.Meter.CreateHistogram<long>(ServiceLevelIndicatorOptions.InstrumentName, "ms", "Duration of the operation.");
    }

    public void Record(string operation, long elapsedTime, params KeyValuePair<string, object?>[] attributes) =>
        Record(operation, ServiceLevelIndicatorOptions.CustomerResourceId, elapsedTime, attributes);

    public void Record(string operation, string customerResourseId, long elapsedTime, params KeyValuePair<string, object?>[] attributes)
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

    public MeasuredOperation StartMeasuring(string operation, params KeyValuePair<string, object?>[] attributes) => new(this, operation, attributes);

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
