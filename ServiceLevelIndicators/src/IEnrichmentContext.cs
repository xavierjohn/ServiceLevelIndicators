namespace ServiceLevelIndicators;

public interface IEnrichmentContext
{
    string Operation { get; }
    void SetCustomerResourceId(string id);
    void AddAttribute(string name, object value);
}
