namespace Asp.ServiceLevelIndicators;

[AttributeUsage(AttributeTargets.Method)]
public class ServiceLevelIndicatorAttribute : Attribute
{
    public string Operation { get; set; } = string.Empty;
}
