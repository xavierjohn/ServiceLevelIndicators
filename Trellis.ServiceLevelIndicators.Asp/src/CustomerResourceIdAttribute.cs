namespace Trellis.ServiceLevelIndicators;

/// <summary>
/// Marks a route parameter as the customer resource identifier for SLI metrics.
/// Only one parameter per endpoint may have this attribute.
/// </summary>
[AttributeUsage(AttributeTargets.Parameter, AllowMultiple = false, Inherited = true)]
public sealed class CustomerResourceIdAttribute : Attribute
{
}
