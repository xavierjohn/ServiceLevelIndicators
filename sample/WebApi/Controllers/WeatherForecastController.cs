namespace SampleWebApplicationSLI.Controllers;

using Microsoft.AspNetCore.Mvc;
using ServiceLevelIndicators;

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
    /// CustomerResourceId = "SampleCustomerResrouceId"
    /// </summary>
    [HttpGet]
    public IEnumerable<WeatherForecast> Get() => GetWeather();

    /// <summary>
    /// Should emit SLI metrics
    /// Operation: "GET WeatherForecast/MyAction1"
    /// CustomerResourceId = "SampleCustomerResrouceId"
    /// </summary>

    [HttpGet("MyAction1")]
    public IEnumerable<WeatherForecast> GetCustom() => GetWeather();

    /// <summary>
    /// Should emit SLI metrics
    /// Operation: "MyOperation"
    /// CustomerResourceId = "SampleCustomerResrouceId"
    /// </summary>
    [HttpGet("MyAction2")]
    [ServiceLevelIndicator(Operation = "MyOperation")]
    public IEnumerable<WeatherForecast> GetOperation() => GetWeather();

    /// <summary>
    /// Should emit SLI metrics
    /// Operation: "GET WeatherForecast/{customerResourceId}"
    /// CustomerResourceId = "Your input"
    /// </summary>
    [HttpGet("{customerResourceId}")]
    public IEnumerable<WeatherForecast> Get([CustomerResourceId] string customerResourceId) => GetWeather();

    private static WeatherForecast[] GetWeather()
    {
        return Enumerable.Range(1, 5).Select(index => new WeatherForecast
        {
            Date = DateTime.Now.AddDays(index),
            TemperatureC = Random.Shared.Next(-20, 55),
            Summary = Summaries[Random.Shared.Next(Summaries.Length)]
        })
        .ToArray();
    }
}
