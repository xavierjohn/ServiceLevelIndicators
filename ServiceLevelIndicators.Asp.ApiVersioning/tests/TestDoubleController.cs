namespace ServiceLevelIndicators.Asp.ApiVersioning.Tests;

using Microsoft.AspNetCore.Mvc;
using global::Asp.Versioning;

[Route("[controller]")]
[ApiVersion("2023-08-29")]
[ApiVersion("2023-09-01")]
public class TestDoubleController : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get()
    {
        await Task.Delay(1);
        return Ok("Hello World!");
    }
}
