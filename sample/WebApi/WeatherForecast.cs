namespace SampleWebApplicationSLI
{
    /// <summary>
    /// Weather Forecast
    /// </summary>
    public class WeatherForecast
    {
        /// <summary>
        /// Date of recording
        /// </summary>
        public DateTime Date { get; set; }

        /// <summary>
        /// Temperature in Centigrade.
        /// </summary>
        public int TemperatureC { get; set; }

        /// <summary>
        /// Temperature in Fahrenheit.
        /// </summary>
        public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);

        /// <summary>
        /// Temperature feeling.
        /// </summary>
        public string? Summary { get; set; }
    }
}
