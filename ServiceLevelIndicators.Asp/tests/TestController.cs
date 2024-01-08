namespace ServiceLevelIndicators.Asp.Tests;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("[controller]")]
public class TestController : ControllerBase
{
    [HttpGet]
    public IActionResult Get() => Ok("Hello World!");

    [HttpPost]
    public IActionResult Post() => Ok("Hello World!");

    [HttpGet("bad_request")]
    public IActionResult Bad() => BadRequest("Sad World!");

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

    [HttpGet("try_get_measured_operation_latency/{value}")]
    public IActionResult TryGetMeasuredOperationLatency(string value)
    {
        if (HttpContext.TryGetMeasuredOperationLatency(out var measuredOperationLatency))
        {
            measuredOperationLatency.AddAttribute("CustomAttribute", value);
            return Ok(true);
        }
        return Ok(false);
    }

    [HttpGet("send_sli")]
    [ServiceLevelIndicator]
    public IActionResult SendSLI() => Ok("Hello");

    [HttpGet("name/{first}/{surname}/{age}")]
    public IActionResult GetCustomerResourceId([Measure] string first, [CustomerResourceId] string last, [Measure] int age) => Ok(first + " " + last + " " + age);
}
