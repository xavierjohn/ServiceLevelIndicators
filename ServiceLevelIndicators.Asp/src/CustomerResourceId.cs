namespace ServiceLevelIndicators;

public class CustomerResourceId(string routeParameterName)
{
    public string RouteParameterName { get; } = routeParameterName;
}
