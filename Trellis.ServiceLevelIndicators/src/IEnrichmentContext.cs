namespace Trellis.ServiceLevelIndicators;

/// <summary>
/// Context provided to enrichment callbacks, allowing attributes to be added to a measurement.
/// </summary>
public interface IEnrichmentContext
{
    /// <summary>
    /// Gets the operation name.
    /// </summary>
    string Operation { get; }

    /// <summary>
    /// Overrides the customer resource identifier for this measurement.
    /// </summary>
    /// <param name="id">The customer resource identifier.</param>
    void SetCustomerResourceId(string id);

    /// <summary>
    /// Adds a custom attribute to the measurement.
    /// </summary>
    /// <param name="name">The attribute name.</param>
    /// <param name="value">The attribute value.</param>
    void AddAttribute(string name, object? value);
}
