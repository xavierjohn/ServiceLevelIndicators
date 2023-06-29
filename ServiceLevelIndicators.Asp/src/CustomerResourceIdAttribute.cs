namespace ServiceLevelIndicators.Asp;
using System;

[AttributeUsage(AttributeTargets.Field)]
public class CustomerResourceIdAttribute : Attribute
{
    public CustomerResourceIdAttribute(string resourceId)
    {
        ResourceId = resourceId;
    }

    public string ResourceId { get; }
}

