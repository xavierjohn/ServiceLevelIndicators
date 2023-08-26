namespace ServiceLevelIndicators.Asp.Tests;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

public class ApplicationPartsLogger : IHostedService
{
    private readonly ILogger<ApplicationPartsLogger> _logger;
    private readonly ApplicationPartManager _partManager;

    public ApplicationPartsLogger(ILogger<ApplicationPartsLogger> logger, ApplicationPartManager partManager)
    {
        _logger = logger;
        _partManager = partManager;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        // Get the names of all the application parts. This is the short assembly name for AssemblyParts
        var applicationParts = _partManager.ApplicationParts.Select(x => x.Name);

        // Create a controller feature, and populate it from the application parts
        var controllerFeature = new ControllerFeature();
        _partManager.PopulateFeature(controllerFeature);

        // Get the names of all of the controllers
        var controllers = controllerFeature.Controllers.Select(x => x.Name);

        // Log the application parts and controllers
#pragma warning disable CA1848 // Use the LoggerMessage delegates
        _logger.LogInformation("Found the following application parts: '{ApplicationParts}' with the following controllers: '{Controllers}'", string.Join(", ", applicationParts), string.Join(", ", controllers));
#pragma warning restore CA1848 // Use the LoggerMessage delegates

        return Task.CompletedTask;
    }

    // Required by the interface
    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}

