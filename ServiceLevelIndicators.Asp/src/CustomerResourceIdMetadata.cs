namespace ServiceLevelIndicators;

public class CustomerResourceIdMetadata(string routeValueName)
{
    public string RouteValueName { get; } = routeValueName;
}
