namespace SampleVersionedWebApplicationSLI.Controllers._2023_06_06;

using Asp.Versioning;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using ServiceLevelIndicators;

/// <summary>
/// Weather forecast controller.
/// </summary>
[ApiController]
[ApiVersion("2023-06-06")]
[ApiVersion("2023-08-06")]
[Route("weather-forecast")]
[Produces("application/json")]
public class WeatherForecastController : ControllerBase
{
    private static readonly string[] Summaries = new[]
    {
    "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
    };
    private readonly ServiceLevelIndicator _serviceLevelIndicator;

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="serviceLevelIndicator"></param>
    public WeatherForecastController(ServiceLevelIndicator serviceLevelIndicator) => _serviceLevelIndicator = serviceLevelIndicator;

    /// <summary>
    /// Should emit SLI metrics
    /// Operation: "GET weather-forecast"
    /// CustomerResourceId = {Calling Service Id}
    /// </summary>
    [HttpGet]
    public IEnumerable<WeatherForecast> Get() => GetWeather();

    /// <summary>
    /// Should emit SLI metrics
    /// Operation: "GET weather-forecast/MyAction1"
    /// CustomerResourceId = {Calling Service Id}
    /// </summary>

    [HttpGet("MyAction1")]
    public IEnumerable<WeatherForecast> GetCustom() => GetWeather();

    /// <summary>
    /// Should emit SLI metrics
    /// Operation: "MyOperation"
    /// CustomerResourceId = {Calling Service Id}
    /// </summary>
    [HttpGet("MyAction2")]
    [ServiceLevelIndicator(Operation = "MyOperation")]
    public IEnumerable<WeatherForecast> GetOperation() => GetWeather();

    /// <summary>
    /// Use Feature to set CustomerResourceId
    /// Operation: "GET weather-forecast/{customerResourceId}"
    /// CustomerResourceId = "Your input"
    /// </summary>
    [HttpGet("{customerResourceId}")]
    public IEnumerable<WeatherForecast> Get([CustomerResourceId] string customerResourceId) => GetWeather();

    /// <summary>
    /// Use Attribute to set CustomerResourceId
    /// Operation: "GET weather-forecast/get-by-zip-code/{zipCode}"
    /// CustomerResourceId is set to the zip code.
    /// </summary>
    [HttpGet("get-by-zip-code/{zipCode}")]
    public IEnumerable<WeatherForecast> GetByZipcode([CustomerResourceId] string zipCode) => GetWeather();

    /// <summary>
    /// Use Attribute to set CustomerResourceId
    /// Operation: "GET weather-forecast/get-by-city/{city}"
    /// CustomerResourceId is set to the city code.
    /// </summary>
    [HttpGet("get-by-city/{city}")]
    public IEnumerable<WeatherForecast> GetByCity([CustomerResourceId] string city) => GetWeather();

    /// <summary>
    /// Background work for given seconds
    /// </summary>
    /// <param name="seconds">Seconds to wait.</param>
    /// <returns></returns>
    [HttpGet("background/{seconds}")]
    [ServiceLevelIndicator("background_work")]
    public Task BackgroundProcess([Measure("wait_seconds")] int seconds) => Task.Delay(TimeSpan.FromSeconds(seconds));

    /// <summary>
    /// Add custom attribute to SLI metrics.
    /// </summary>
    /// <param name="attribute">Name of the attribute. https://opentelemetry.io/docs/specs/otel/common/attribute-naming/</param>
    /// <param name="value">Attribute Value.</param>
    /// <returns></returns>
    [HttpGet("{attribute}/{value}")]
    public int CustomAttribute(string attribute, string value)
    {
        HttpContext.GetMeasuredOperationLatency().AddAttribute(attribute, value);
        return 7;
    }

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
