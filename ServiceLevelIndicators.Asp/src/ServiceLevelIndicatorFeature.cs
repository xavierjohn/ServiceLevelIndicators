namespace ServiceLevelIndicators;

using System.Collections.Generic;

internal sealed class ServiceLevelIndicatorFeature : IServiceLevelIndicatorFeature
{
    public ServiceLevelIndicatorFeature(string customerResourceId) => CustomerResourceId = customerResourceId;

    public string CustomerResourceId { get; set; }

    public IList<KeyValuePair<string, object?>> Attributes { get; } = new List<KeyValuePair<string, object?>>();

    public void AddAttribute(string attribute, object? value) => Attributes.Add(new KeyValuePair<string, object?>(attribute, value));
}
