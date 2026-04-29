namespace SampleWebApplicationSLI.Controllers;

using Microsoft.AspNetCore.Mvc;
using Trellis.ServiceLevelIndicators;

/// <summary>
/// Weather forecast controller.
/// </summary>
[ApiController]
[Route("[controller]")]
[Produces("application/json")]
public class WeatherForecastController : ControllerBase
{
    private static readonly string[] Summaries = new[]
    {
    "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
};

    /// <summary>
    /// Should emit SLI metrics
    /// Operation: "GET WeatherForecast"
    /// CustomerResourceId = "SampleCustomerResourceId"
    /// Outcome = "Success"
    /// </summary>
    [HttpGet]
    public IEnumerable<WeatherForecast> Get() => GetWeather();

    /// <summary>
    /// Should emit SLI metrics
    /// Operation: "GET WeatherForecast/MyAction1"
    /// CustomerResourceId = "SampleCustomerResourceId"
    /// Outcome = "Success"
    /// </summary>

    [HttpGet("MyAction1")]
    public IEnumerable<WeatherForecast> GetCustom() => GetWeather();

    /// <summary>
    /// Should emit SLI metrics
    /// Operation: "MyOperation"
    /// CustomerResourceId = "SampleCustomerResourceId"
    /// Outcome = "Success"
    /// </summary>
    [HttpGet("MyAction2")]
    [ServiceLevelIndicator(Operation = "MyOperation")]
    public IEnumerable<WeatherForecast> GetOperation() => GetWeather();

    /// <summary>
    /// Should emit SLI metrics
    /// Operation: "GET WeatherForecast/{customerResourceId}"
    /// CustomerResourceId = "Your input"
    /// Outcome = "Success"
    /// </summary>
    [HttpGet("{customerResourceId}")]
    public IEnumerable<WeatherForecast> Get([CustomerResourceId] string customerResourceId) => GetWeather();

    /// <summary>
    /// Demo endpoint that emits Outcome = "ClientError".
    /// </summary>
    [HttpGet("demo/client-error/{customerResourceId}")]
    public IActionResult ClientError([CustomerResourceId] string customerResourceId) =>
        BadRequest(new { customerResourceId, error = "Invalid forecast request." });

    /// <summary>
    /// Demo endpoint that emits Outcome = "Failure" because 429 is service-impacting by default.
    /// </summary>
    [HttpGet("demo/throttled/{customerResourceId}")]
    public IActionResult Throttled([CustomerResourceId] string customerResourceId) =>
        StatusCode(StatusCodes.Status429TooManyRequests, new { customerResourceId, error = "Too many forecast requests." });

    /// <summary>
    /// Demo endpoint that emits Outcome = "Failure".
    /// </summary>
    [HttpGet("demo/server-error/{customerResourceId}")]
    public IActionResult ServerError([CustomerResourceId] string customerResourceId) =>
        StatusCode(StatusCodes.Status500InternalServerError, new { customerResourceId, error = "Forecast service unavailable." });

    private static WeatherForecast[] GetWeather() => Enumerable.Range(1, 5).Select(index => new WeatherForecast
    {
        Date = DateTime.Now.AddDays(index),
        TemperatureC = Random.Shared.Next(-20, 55),
        Summary = Summaries[Random.Shared.Next(Summaries.Length)]
    })
        .ToArray();
}
