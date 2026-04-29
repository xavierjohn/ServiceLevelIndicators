namespace Trellis.ServiceLevelIndicators;

/// <summary>
/// Endpoint metadata indicating which route value supplies the customer resource identifier.
/// </summary>
public sealed class CustomerResourceIdMetadata(string routeValueName)
{
    /// <summary>
    /// Gets the route value name mapped to the customer resource identifier.
    /// </summary>
    public string RouteValueName { get; } = routeValueName;
}
