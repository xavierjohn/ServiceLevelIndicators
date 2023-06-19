namespace Asp.SampleVersionedWebApplicationSLI.Controllers.Neutral;

using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;

/// <summary>
/// Health controller.
/// </summary>
[Route("[controller]")]
[ApiVersionNeutral]
[ApiController]
public class HealthController : ControllerBase
{
    /// <summary>
    /// Get health.
    /// </summary>
    /// <returns>Health status.</returns>
    [HttpGet]
    public IActionResult Get() => Ok("Healthy");
}
