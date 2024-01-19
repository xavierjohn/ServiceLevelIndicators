namespace ServiceLevelIndicators;

public interface IMeasurementContext
{
    string Operation { get; }
    void SetCustomerResourceId(string id);
    void AddAttribute(string name, object value);
}
