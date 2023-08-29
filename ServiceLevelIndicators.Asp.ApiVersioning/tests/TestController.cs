namespace ServiceLevelIndicators.Asp.ApiVersioning.Tests;

using Asp.Versioning;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

[Route("[controller]")]
[ApiVersion("2023-8-29")]
public class TestController : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get()
    {
        await Task.Delay(1);
        return Ok("Hello World!");
    }
}
