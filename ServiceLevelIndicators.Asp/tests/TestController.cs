namespace ServiceLevelIndicators.Asp.Tests;
using Microsoft.AspNetCore.Mvc;

[Route("[controller]")]
public class TestController : ControllerBase
{
    [HttpGet]
    public IActionResult Get() => Ok("Hello World!");

    [HttpGet("operation")]
    [ServiceLevelIndicator(Operation = "TestOperation")]
    public IActionResult GetOperation() => Ok("Hello World!");
}
