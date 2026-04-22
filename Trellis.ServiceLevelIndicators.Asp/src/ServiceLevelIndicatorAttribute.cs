namespace Trellis.ServiceLevelIndicators;

/// <summary>
/// Attribute to mark a controller or action as emitting service level indicator metrics.
/// When <see cref="ServiceLevelIndicatorOptions.AutomaticallyEmitted"/> is false,
/// only actions decorated with this attribute will emit metrics.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public sealed class ServiceLevelIndicatorAttribute : Attribute
{
    public ServiceLevelIndicatorAttribute() { }

    public ServiceLevelIndicatorAttribute(string operation) => Operation = operation;

    public string? Operation { get; set; }
}