namespace ServiceLevelIndicators;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class ServiceLevelIndicatorAttribute : Attribute
{
    public ServiceLevelIndicatorAttribute() { }

    public ServiceLevelIndicatorAttribute(string operation) => Operation = operation;

    public string? Operation { get; set; }
}
