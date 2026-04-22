namespace Trellis.ServiceLevelIndicators.Asp.Tests;

using System;
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

    [HttpGet("server_error")]
    public IActionResult ServerError() => StatusCode(500, "Server Error!");

    [HttpGet("throw")]
    public IActionResult Throw()
    {
        _ = HttpContext;
        throw new InvalidOperationException("Boom");
    }

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

    [HttpGet("try_get_measured_operation/{value}")]
    public IActionResult TryGetMeasuredOperation(string value)
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
}