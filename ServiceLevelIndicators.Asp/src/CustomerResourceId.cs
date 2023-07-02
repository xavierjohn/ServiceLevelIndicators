namespace ServiceLevelIndicators;
public class CustomerResourceId
{
    public CustomerResourceId(string routeParameterName) => RouteParameterName = routeParameterName;
    public string RouteParameterName { get; }
}
