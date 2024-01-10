namespace ServiceLevelIndicators;

public sealed class MeasureMetadata(string routeValueName, string? attributeName = default)
{
    public string RouteValueName { get; } = routeValueName;

    public string AttributeName { get; } = attributeName ?? routeValueName;
}
