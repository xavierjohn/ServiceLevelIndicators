namespace ServiceLevelIndicators;

using Microsoft.AspNetCore.Mvc.ApplicationModels;

internal sealed class ServiceLevelIndicatorConvention : IParameterModelConvention
{
    public void Apply(ParameterModel parameter)
    {
        var selectors = parameter.Action.Selectors;

        if (selectors.Count == 0)
        {
            selectors.Add(new());
        }

        for (var i = 0; i < parameter.Attributes.Count; i++)
        {
            switch (parameter.Attributes[i])
            {
                case CustomerResourceIdAttribute:
                    foreach (var selector in selectors)
                    {
                        if (selector.EndpointMetadata.OfType<CustomerResourceIdMetadata>().Any())
                            throw new InvalidOperationException("Multiple " + nameof(CustomerResourceIdAttribute) + " defined on action '" + parameter.Action.DisplayName + "'.");
                        selector.EndpointMetadata.Add(new CustomerResourceIdMetadata(parameter.Name));
                    }

                    break;
                case MeasureAttribute measure:
                    foreach (var selector in selectors)
                    {
                        selector.EndpointMetadata.Add(new MeasureMetadata(parameter.Name, measure.Name));
                    }

                    break;
            }
        }
    }
}