namespace SampleWebApplicationSLI.Controllers;

using Microsoft.AspNetCore.Mvc;
using ServiceLevelIndicators;
using Microsoft.AspNetCore.Http;
using Asp.Versioning;
using Microsoft.AspNetCore.Http.Features;
using ServiceLevelIndicators.Asp;

/// <summary>
/// Weather forecast controller.
/// </summary>
[ApiController]
[ApiVersion("2023-06-06")]
[ApiVersion("2023-08-06")]
[Route("[controller]")]
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
    public IEnumerable<WeatherForecast> Get(string customerResourceId)
    {
        var sliFeature = HttpContext.Features.GetRequiredFeature<IServiceLevelIndicatorFeature>();
        sliFeature.MeasureOperationLatency.CustomerResourceId = customerResourceId;

        return GetWeather();
    }

    /// <summary>
    /// Should emit SLI metrics
    /// Operation: "GET WeatherForecast/{customerResourceId}"
    /// CustomerResourceId = "Your input"
    /// </summary>
    [HttpGet("attrib/{customerResourceId}")]
    public IEnumerable<WeatherForecast> GetAttrib([CustomerResourceId] string customerResourceId) => GetWeather();

    /// <summary>
    /// Background work for given seconds
    /// </summary>
    /// <param name="seconds">Seconds to wait.</param>
    /// <returns></returns>
    [HttpGet("background/{seconds}")]
    public async Task BackgroundProcess(int seconds)
    {
        var attribute = new KeyValuePair<string, object?>("wait_seconds", seconds);
        using var measuredOperation = _serviceLevelIndicator.StartLatencyMeasureOperation("background_work", attribute);
        await Task.Delay(TimeSpan.FromSeconds(seconds));
        measuredOperation.SetState(System.Diagnostics.ActivityStatusCode.Ok);
    }

    /// <summary>
    /// Add custom attribute to SLI metrics.
    /// </summary>
    /// <param name="attribute">Name of the attribute. https://opentelemetry.io/docs/specs/otel/common/attribute-naming/</param>
    /// <param name="value">Attribute Value.</param>
    /// <returns></returns>
    [HttpGet("{attribute}/{value}")]
    public int CustomAttribute(string attribute, string value)
    {
        var sliFeature = HttpContext.Features.GetRequiredFeature<IServiceLevelIndicatorFeature>();
        sliFeature.MeasureOperationLatency.AddAttribute(attribute, value);
        return 7;
    }

    private static IEnumerable<WeatherForecast> GetWeather()
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
