namespace Asp.ServiceLevelIndicators;

internal sealed class ServiceLevelIndicatorFeature : IServiceLevelIndicatorFeature
{
    public ServiceLevelIndicatorFeature(string customerResourceId) => CustomerResourceId = customerResourceId;

    public string CustomerResourceId { get; set; }
}
