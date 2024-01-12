namespace ServiceLevelIndicators;
public interface IMeasurementContext
{
    void AddAttribute(string name, object value);
}
