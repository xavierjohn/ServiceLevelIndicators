﻿namespace ServiceLevelIndicators;

using System.Diagnostics.Metrics;

/// <summary>
/// Options for configuring the Service Level Indicator.
/// DefaultCustomerResourceId & LocationId are mandatory properties.
/// </summary>
public class ServiceLevelIndicatorOptions
{
    private readonly object _meterLock = new();
    private Meter _meter = null!;

    /// <summary>
    /// The meter that is used to create the histogram that reports the latency.
    /// </summary>
    public Meter Meter
    {
        get
        {
            lock (_meterLock)
                return _meter;
        }
        set
        {
            lock (_meterLock)
                _meter = value;
        }
    }

    /// <summary>
    /// CustomerResrouceId is the unique identifier for the customer like subscriptionId, tenantId, etc.
    /// CustomerResourceId can be set for the entire service here or in each API method.
    /// </summary>
    public string CustomerResourceId { get; set; } = "Unset";

    /// <summary>
    /// Location where the service is running.
    /// </summary>
    public string LocationId { get; set; } = string.Empty;

    /// <summary>
    /// The instrument name created on the given meter. Cannot be null.
    /// </summary>
    public string DurationInstrumentName { get; set; } = "operation.duration";

    /// <summary>
    /// Activity Status Code attribute name.
    /// [ActivityStatusCode](https://learn.microsoft.com/en-us/dotnet/api/system.diagnostics.activitystatuscode?view=net-7.0)
    /// </summary>
    public string ActivityStatusCodeAttributeName { get; set; } = "activity.status.code";

    /// <summary>
    /// Automatically emit for all API methods.
    /// If false, use the ServiceLevelIndicator Attribute to emit.
    /// </summary>
    public bool AutomaticallyEmitted { get; set; } = true;
}
