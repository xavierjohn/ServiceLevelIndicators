namespace Trellis.ServiceLevelIndicators.Asp.Tests;

using System.Diagnostics.Metrics;
using System.Net;
using System.Reflection;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

/// <summary>
/// Regression tests covering interop between <see cref="ServiceLevelIndicatorServiceCollectionExtensions.AddMvc"/>
/// and the ASP.NET Core <c>IProblemDetailsService</c> pipeline registered via <c>AddProblemDetails()</c>.
///
/// Historical bug: <c>AddMvc()</c> previously called <c>services.AddMvcCore(...)</c> just to register a single
/// MVC convention. Re-invoking the MVC services pipeline after the host had already called
/// <c>AddControllers()</c> + <c>AddProblemDetails()</c> + <c>AddApiVersioning()...AddOpenApi()</c>
/// caused the <c>IProblemDetailsService</c> writer to drop the runtime-typed <c>Errors</c> dictionary
/// from <see cref="HttpValidationProblemDetails"/> responses (the writer fell back to the static base
/// <see cref="ProblemDetails"/> type for serialization). Extensions on the base type (<c>traceId</c>, custom
/// keys) survived; the validation <c>errors</c> dictionary did not.
///
/// The fix replaces <c>AddMvcCore(...)</c> with <c>Configure&lt;MvcOptions&gt;(...)</c>, which registers the
/// convention without re-invoking the MVC services pipeline.
/// </summary>
public class ProblemDetailsInteropTests
{
    [Fact]
    public void AddMvc_registers_ServiceLevelIndicatorConvention_without_calling_AddMvcCore()
    {
        // Baseline: capture the set of services AddMvcCore would have introduced.
        var withMvcCore = new ServiceCollection();
        withMvcCore.AddMvcCore();
        var mvcCoreOnlyTypes = withMvcCore
            .Select(d => d.ServiceType.FullName)
            .Where(name => name is not null)
            .ToHashSet();

        // Subject: a service collection with ONLY AddServiceLevelIndicator().AddMvc().
        using var meter = new Meter(nameof(ProblemDetailsInteropTests));
        var services = new ServiceCollection();
        services.AddOptions();
        services.AddServiceLevelIndicator(options =>
        {
            options.Meter = meter;
            options.CustomerResourceId = "TestCustomerResourceId";
            options.LocationId = ServiceLevelIndicator.CreateLocationId("public", "West US 3");
        }).AddMvc();

        // The convention must be registered (functional check). MvcOptions.Conventions is
        // IList<IApplicationModelConvention>; ASP.NET wraps an IParameterModelConvention in an
        // internal adapter when added, so we can't pattern-match the stored instance directly.
        // Instead, inspect each convention's fields for a wrapped ServiceLevelIndicatorConvention.
        using var provider = services.BuildServiceProvider();
        var mvcOptions = provider.GetRequiredService<IOptions<MvcOptions>>().Value;
        mvcOptions.Conventions.Should().Contain(
            convention => WrapsServiceLevelIndicatorConvention(convention),
            "AddMvc() must register the ServiceLevelIndicatorConvention.");

        // AddMvc() must NOT pull in the rest of MvcCore's service registrations. If any MvcCore-only
        // service types appear in our subject collection, AddMvc() is calling AddMvcCore() under the
        // hood and will interfere with the host's already-configured MVC + ProblemDetails pipeline.
        var subjectTypes = services
            .Select(d => d.ServiceType.FullName)
            .Where(name => name is not null)
            .ToHashSet();

        var leakedMvcCoreServices = subjectTypes
            .Intersect(mvcCoreOnlyTypes)
            .Where(t => t!.StartsWith("Microsoft.AspNetCore.Mvc", StringComparison.Ordinal))
            .ToList();

        leakedMvcCoreServices.Should().BeEmpty(
            "AddMvc() must register only the convention, not invoke AddMvcCore(). " +
            "Re-invoking the MVC services pipeline interferes with IProblemDetailsService " +
            "polymorphic serialization of HttpValidationProblemDetails.");

        // Targeted regression check: AddMvcCore() registers MVC's IProblemDetailsWriter
        // (Microsoft.AspNetCore.Http namespace, so missed by the prefix filter above). That writer
        // is the exact service whose stale registration caused the original 'errors'-stripping bug.
        services.Should().NotContain(
            d => d.ServiceType.FullName == "Microsoft.AspNetCore.Http.IProblemDetailsWriter",
            "AddMvc() must not introduce IProblemDetailsWriter — that's what caused the 422 'errors' dict to be dropped.");
    }

    [Fact]
    public async Task AddMvc_validation_problem_includes_errors_when_written_via_ProblemDetailsService()
    {
        using var meter = new Meter(nameof(ProblemDetailsInteropTests));
        using var host = await CreateHost(meter);

        var ct = TestContext.Current.CancellationToken;
        var client = host.GetTestClient();
        var response = await client.PostAsync("/problem/validate", content: null, ct);

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        var body = await response.Content.ReadAsStringAsync(ct);

        using var doc = JsonDocument.Parse(body);
        var root = doc.RootElement;

        root.TryGetProperty("status", out var status).Should().BeTrue();
        status.GetInt32().Should().Be(StatusCodes.Status422UnprocessableEntity);

        root.TryGetProperty("errors", out var errors).Should().BeTrue(
            $"Validation 'errors' dictionary must round-trip through IProblemDetailsService. Body: {body}");
        errors.ValueKind.Should().Be(JsonValueKind.Object);
        errors.TryGetProperty("name", out _).Should().BeTrue($"Body: {body}");
    }

    private static async Task<IHost> CreateHost(Meter meter) =>
        await new HostBuilder()
            .ConfigureWebHost(webBuilder => webBuilder
                .UseTestServer()
                .ConfigureServices(services =>
                {
                    services.AddProblemDetails();
                    services.AddControllers();
                    services.AddServiceLevelIndicator(options =>
                    {
                        options.Meter = meter;
                        options.CustomerResourceId = "TestCustomerResourceId";
                        options.LocationId = ServiceLevelIndicator.CreateLocationId("public", "West US 3");
                    }).AddMvc();
                })
                .Configure(app => app
                    .UseRouting()
                    .UseServiceLevelIndicator()
                    .UseEndpoints(endpoints => endpoints.MapControllers())))
            .StartAsync();

    private static bool WrapsServiceLevelIndicatorConvention(IApplicationModelConvention convention)
    {
        if ((object)convention is ServiceLevelIndicatorConvention)
            return true;

        // ASP.NET wraps non-IApplicationModelConvention conventions (e.g. IParameterModelConvention)
        // in an internal adapter that holds the inner convention in a private field.
        const BindingFlags bindingFlags = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;
        return convention.GetType()
            .GetFields(bindingFlags)
            .Any(f => f.GetValue(convention) is ServiceLevelIndicatorConvention);
    }
}

[ApiController]
[Route("problem")]
public sealed class ProblemDetailsTestController : ControllerBase
{
    // Mirrors what frameworks like Trellis.Asp's ResponseFailureWriter do: invoke
    // Results.ValidationProblem(...).ExecuteAsync(httpContext), which writes via IProblemDetailsService.
    [HttpPost("validate")]
    public async Task Validate()
    {
        var errors = new Dictionary<string, string[]> { ["name"] = ["Name is required."] };
        var result = Results.ValidationProblem(errors, statusCode: StatusCodes.Status422UnprocessableEntity);
        await result.ExecuteAsync(HttpContext);
    }
}
