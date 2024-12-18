﻿namespace SampleMinimalApiSli;
using ServiceLevelIndicators;

/// <summary>
/// User API routes
/// </summary>
public static class UserExt
{
    /// <summary>
    /// Use User Route
    /// </summary>
    /// <param name="app"></param>
    public static void UseUserRoute(this WebApplication app)
    {
        var userApi = app.MapGroup("/users")
            .AddServiceLevelIndicator();


        userApi.MapGet("/", () => "Hello Users");

        userApi.MapGet("/{name}", (string name) => $"Hello {name}").WithName("GetUserById");
    }

}
