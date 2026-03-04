namespace ServiceLevelIndicators.Asp.ApiVersioning.Tests;

using global::Asp.Versioning;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("[controller]")]
[ApiVersion("2023-08-29")]
public class TestSingleController : ControllerBase
{
    [HttpGet]
    public IActionResult Get() => Ok("Hello World!");
}