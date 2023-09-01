namespace ServiceLevelIndicators.Asp.ApiVersioning.Tests;

using Microsoft.AspNetCore.Mvc;
using global::Asp.Versioning;

[ApiController]
[Route("[controller]")]
[ApiVersionNeutral]
public class TestNeutralController : ControllerBase
{
    [HttpGet]
    public IActionResult Get() => Ok("Hello World!");
}
