namespace ServiceLevelIndicators;

using Microsoft.AspNetCore.Mvc.ApplicationModels;

internal sealed class ServiceLevelIndicatorConvention : IParameterModelConvention
{
    public void Apply(ParameterModel parameter)
    {
        var selectors = parameter.Action.Selectors;
        SelectorModel selector;

        if (selectors.Count == 0)
        {
            selectors.Add(selector = new());
        }
        else
        {
            selector = selectors[0];
        }

        for (var i = 0; i < parameter.Attributes.Count; i++)
        {
            switch (parameter.Attributes[i])
            {
                case CustomerResourceIdAttribute:
                    // TODO: what happens if there is more than one?
                    selector.EndpointMetadata.Add(new CustomerResourceIdMetadata(parameter.Name));
                    break;
                case MeasureAttribute measure:
                    selector.EndpointMetadata.Add(new MeasureMetadata(parameter.Name, measure.Name));
                    break;
            }
        }
    }
}
