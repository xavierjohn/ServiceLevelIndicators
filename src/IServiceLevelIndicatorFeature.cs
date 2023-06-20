namespace Asp.ServiceLevelIndicators;
/// <summary>
/// A feature for setting up service level indicators.
/// </summary>
public interface IServiceLevelIndicatorFeature
{
    /// <summary>
    /// Gets or sets the customer resource id.
    /// </summary>
    string CustomerResourceId { get; set; }

    /// <summary>
    /// Additional attributes.
    /// https://opentelemetry.io/docs/specs/otel/common/#attribute
    /// </summary>
    IList<KeyValuePair<string, object?>> Attributes { get; }

    void AddAttribute(string attribute, object? value);
}
