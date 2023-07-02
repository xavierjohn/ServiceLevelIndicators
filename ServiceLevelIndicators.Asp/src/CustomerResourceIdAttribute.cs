namespace ServiceLevelIndicators;

using Microsoft.AspNetCore.Mvc.ApplicationModels;

[AttributeUsage(AttributeTargets.Parameter)]
public class CustomerResourceIdAttribute : Attribute, IParameterModelConvention
{
    public void Apply(ParameterModel parameter)
    {
        var selectors = parameter.Action.Selectors;
        if (selectors.Count == 0)
            selectors.Add(new());

        selectors[0].EndpointMetadata.Add(new CustomerResourceId(parameter.Name));
    }
}

