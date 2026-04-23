namespace Trellis.ServiceLevelIndicators.Asp.Tests;

using System.Reflection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.AspNetCore.Mvc.Controllers;

/// <summary>
/// Controller with two [CustomerResourceId] parameters on a single action.
/// Marked [NonController] so it is not auto-discovered by the default test hosts.
/// </summary>
[NonController]
[Route("[controller]")]
public class MultipleCustomerResourceIdController : ControllerBase
{
    [HttpGet("{a}/{b}")]
    public IActionResult Get([CustomerResourceId] string a, [CustomerResourceId] string b) => Ok(a + b);
}

/// <summary>
/// A feature provider that adds a single controller type, regardless of
/// whether it has <see cref="NonControllerAttribute"/>.
/// </summary>
internal sealed class SingleControllerFeatureProvider(Type controllerType) : IApplicationFeatureProvider<ControllerFeature>
{
    public void PopulateFeature(IEnumerable<ApplicationPart> parts, ControllerFeature feature) =>
        feature.Controllers.Add(controllerType.GetTypeInfo());
}
