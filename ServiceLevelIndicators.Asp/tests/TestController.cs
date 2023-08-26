namespace ServiceLevelIndicators.Asp.Tests;
using Microsoft.AspNetCore.Mvc;

[ApiController]
public class TestController : ControllerBase
{
    [HttpGet]
    public static string Get()
    {
        return "Hello";
    }
}
