namespace ServiceLevelIndicators.Asp.ApiVersioning.Tests;

using Microsoft.AspNetCore.Mvc;
using global::Asp.Versioning;

[ApiController]
[Route("[controller]")]
[ApiVersion("2023-08-29")]
public class TestSingleController : ControllerBase
{
    [HttpGet]
    public IActionResult Get() => Ok("Hello World!");
}
