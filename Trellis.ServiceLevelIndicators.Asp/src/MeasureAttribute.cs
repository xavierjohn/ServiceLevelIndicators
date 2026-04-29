namespace Trellis.ServiceLevelIndicators;

/// <summary>
/// Marks a route parameter as an additional measured attribute emitted with SLI metrics.
/// </summary>
[AttributeUsage(AttributeTargets.Parameter, AllowMultiple = false, Inherited = true)]
public sealed class MeasureAttribute(string? name = default) : Attribute
{
    public string? Name { get; } = name;
}
