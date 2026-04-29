namespace Trellis.ServiceLevelIndicators;

using Microsoft.AspNetCore.Http;

/// <summary>
/// ASP.NET Core-specific SLI options.
/// </summary>
public sealed class ServiceLevelIndicatorHttpOptions
{
    /// <summary>
    /// Optional classifier that maps the completed HTTP request to an SLI outcome.
    /// </summary>
    public Func<HttpContext, SliOutcome>? ClassifyOutcome { get; set; }
}
