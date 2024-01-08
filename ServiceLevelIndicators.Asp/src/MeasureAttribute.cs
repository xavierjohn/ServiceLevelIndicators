namespace ServiceLevelIndicators;

[AttributeUsage(AttributeTargets.Parameter, AllowMultiple = false, Inherited = true)]
public sealed class MeasureAttribute(string? name = default) : Attribute
{
    public string? Name { get; } = name;
}
