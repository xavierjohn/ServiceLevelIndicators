namespace ServiceLevelIndicators;

using System.Reflection;
using Microsoft.AspNetCore.Builder;

public static class EndpointBuilderExtensions
{
    public static IEndpointConventionBuilder AddServiceLevelIndicator(this IEndpointConventionBuilder builder, string? operation = default)
    {
        builder.WithMetadata(new ServiceLevelIndicatorAttribute() { Operation = operation });
        builder.Finally(AddSliMetadata);
        return builder;
    }

    private static void AddSliMetadata(EndpointBuilder endpoint)
    {
        // this appears to be the only way to get back to the original method signature
        // REF: https://github.com/dotnet/aspnetcore/blob/main/src/Http/Routing/src/RouteEndpointDataSource.cs#L183
        if (endpoint.Metadata.OfType<MethodInfo>().FirstOrDefault()?.GetParameters() is not { } parameters)
        {
            return;
        }

        for (var i = 0; i < parameters.Length; i++)
        {
            var parameter = parameters[i];
            var attributes = parameter.GetCustomAttributes(inherit: false);

            for (var j = 0; j < attributes.Length; j++)
            {
                switch (attributes[j])
                {
                    case CustomerResourceIdAttribute:
                        endpoint.Metadata.Add(new CustomerResourceIdMetadata(parameter.Name!));
                        break;
                    case MeasureAttribute measure:
                        endpoint.Metadata.Add(new MeasureMetadata(parameter.Name!, measure.Name));
                        break;
                }
            }
        }
    }
}
