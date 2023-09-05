namespace ServiceLevelIndicators.Asp.Tests;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("[controller]")]
public class TestController : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get()
    {
        await Task.Delay(1);
        return Ok("Hello World!");
    }

    [HttpGet("operation")]
    [ServiceLevelIndicator(Operation = "TestOperation")]
    public IActionResult GetOperation() => Ok("Hello World!");

    [HttpGet("customer_resourceid/{id}")]
    public IActionResult GetCustomerResourceId([CustomerResourceId] string id) => Ok(id);

    [HttpGet("custom_attribute/{value}")]
    public IActionResult AddCustomAttribute(string value)
    {
        HttpContext.GetMeasuredOperationLatency().AddAttribute("CustomAttribute", value);
        return Ok(value);
    }

    [HttpGet("send_sli")]
    [ServiceLevelIndicator]
    public async Task<IActionResult> SendSLI()
    {
        await Task.Delay(1);
        return Ok("Hello");
    }
}
