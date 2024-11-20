namespace ServiceLevelIndicators;

using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Reflection;
using Microsoft.Extensions.Options;

public class ServiceLevelIndicator : IDisposable
{
    public const string InstrumentationName = nameof(ServiceLevelIndicator);

    public ServiceLevelIndicatorOptions ServiceLevelIndicatorOptions { get; }
    internal static readonly AssemblyName AssemblyName = typeof(ServiceLevelIndicator).Assembly.GetName();
    internal static readonly string InstrumentationVersion = AssemblyName.Version!.ToString();
    internal static readonly Meter Meter = new(InstrumentationName, InstrumentationVersion);

    private readonly Histogram<long> _responseLatencyHistogram;
    private bool _disposedValue;

    public ServiceLevelIndicator(IOptions<ServiceLevelIndicatorOptions> options)
    {
        ServiceLevelIndicatorOptions = options.Value;
        _responseLatencyHistogram = Meter.CreateHistogram<long>(ServiceLevelIndicatorOptions.InstrumentName, "ms", "Duration of the operation.");
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

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposedValue)
        {
            if (disposing)
                Meter.Dispose();

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
