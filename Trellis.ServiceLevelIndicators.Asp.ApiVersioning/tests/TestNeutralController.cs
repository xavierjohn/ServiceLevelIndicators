namespace Trellis.ServiceLevelIndicators.Asp.ApiVersioning.Tests;

using global::Asp.Versioning;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("[controller]")]
[ApiVersionNeutral]
public class TestNeutralController : ControllerBase
{
    [HttpGet]
    public IActionResult Get() => Ok("Hello World!");
}
