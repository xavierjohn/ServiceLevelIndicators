namespace Asp.ServiceLevelIndicators;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class ServiceLevelIndicatorAttribute : Attribute
{
    public string? Operation { get; set; }
}
