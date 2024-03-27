namespace SampleVersionedWebApplicationSLI.Controllers._2023_06_06;

using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using ServiceLevelIndicators;
using static System.Runtime.InteropServices.JavaScript.JSType;

/// <summary>
/// Hello World controller.
/// </summary>
[ApiController]
[ApiVersion("1996-06-06")]
[ApiVersion("2023-08-06")]
[Route("hello-world")]
[Produces("application/json")]
public class HelloWorldController : ControllerBase
{
    /// <summary>
    /// hello World.
    /// </summary>
    [HttpGet]
    public ActionResult<string> Get()
    {
        var next = Random.Shared.Next(0, 100);
        if (next < 10) return BadRequest("Sim bad request");
        if (next < 20) return StatusCode(StatusCodes.Status500InternalServerError, "Sim Server error");
        return Ok("Hello World");
    }

    /// <summary>
    /// Hello world with name.
    /// </summary>
    /// <param name="name"></param>
    /// <returns></returns>
    [HttpGet("{name}")]
    public ActionResult<string> GetCustom([CustomerResourceId] string name)
    {
        var next = Random.Shared.Next(0, 100);
        if (next < 10) return BadRequest("Sim bad request");
        if (next < 20) return StatusCode(StatusCodes.Status500InternalServerError, "Sim Server error");
        return Ok("Hello World " + name);
    }
}
