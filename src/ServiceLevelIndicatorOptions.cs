namespace Asp.ServiceLevelIndicators;

using System.Diagnostics.Metrics;

/// <summary>
/// Options for configuring the Service Level Indicator.
/// DefaultCustomerResourceId & LocationId are mandatory properties.
/// </summary>
public class ServiceLevelIndicatorOptions
{
    /// <summary>
    /// DefaultCustomerResourceId is used if CustomerResourceId is not set by the API.
    /// </summary>
    public string DefaultCustomerResourceId { get; set; } = string.Empty;

    /// <summary>
    /// Location where the service is running.
    /// </summary>
    public string LocationId { get; set; } = string.Empty;

    /// <summary>
    /// The instrument name created on the given meter. Cannot be null.
    /// </summary>
    public string InstrumentName { get; set; } = "LatencySLI";
}
