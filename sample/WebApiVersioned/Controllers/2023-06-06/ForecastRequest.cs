namespace SampleVersionedWebApplicationSLI.Controllers._2023_06_06;

using ServiceLevelIndicators;

/// <summary>
/// Weather forecast request.
/// </summary>
public class ForecastRequest
{
    /// <summary>
    /// Zip code
    /// </summary>
    [CustomerResourceId]
    public string ZipCode { get; set; } = string.Empty;

    /// <summary>
    /// Some random data
    /// </summary>
    public string Data { get; set; } = string.Empty;
}
