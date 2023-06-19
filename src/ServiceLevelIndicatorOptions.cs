namespace Asp.ServiceLevelIndicators;

using System.Diagnostics.Metrics;

/// <summary>
/// Options for configuring the Service Level Indicator.
/// DefaultCustomerResourceId & LocationId are mandatory properties.
/// </summary>
public class ServiceLevelIndicatorOptions
{
    /// <summary>
    /// CustomerResrouceId is the unique identifier for the customer like subscriptionId, tenantId, etc.
    /// CustomerResourceId can be set for the entire service here or in each API method.
    /// </summary>
    public string CustomerResourceId { get; set; } = string.Empty;

    /// <summary>
    /// Location where the service is running.
    /// </summary>
    public string LocationId { get; set; } = string.Empty;

    /// <summary>
    /// The instrument name created on the given meter. Cannot be null.
    /// </summary>
    public string InstrumentName { get; set; } = "LatencySLI";

    /// <summary>
    /// Automatically emit for all API methods.
    /// If false, use the ServiceLevelIndicator Attribute to emit.
    /// </summary>
    public bool AutomaticallyEmitted { get; set; } = true;
}
