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
        HttpContext.GetMeasuredOperation().AddAttribute("CustomAttribute", value);
        return Ok(value);
    }

    [HttpGet("try_get_measured_operation_latency/{value}")]
    public IActionResult TryGetMeasuredOperationLatency(string value)
    {
        if (HttpContext.TryGetMeasuredOperation(out var measuredOperation))
        {
            measuredOperation.AddAttribute("CustomAttribute", value);
            return Ok(true);
        }
        return Ok(false);
    }

    [HttpGet("send_sli")]
    [ServiceLevelIndicator]
    public IActionResult SendSLI() => Ok("Hello");

    [HttpGet("name/{first}/{surname}/{age}")]
    public IActionResult GetCustomerResourceId([Measure] string first, [CustomerResourceId] string surname, [Measure] int age) => Ok(first + " " + surname + " " + age);

    [HttpGet("multiple_customer_resource_id/{first}/{surname}")]
    public IActionResult MultipleCustomerResourceId([CustomerResourceId] string first, [CustomerResourceId] string surname) => Ok(first + " " + surname);
}
