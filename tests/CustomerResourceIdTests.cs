namespace Asp.ServiceLevelIndicators.Tests;

using FluentAssertions;
using Xunit;

public class CustomerResourceIdTests
{
    [Fact]
    public void Will_create_CustomerResourceId()
    {
        // Arrange
        var serviceId = Guid.Parse("33f319bd-7b57-4996-8c78-eaadba297b51");

        // Act
        var actual = ServiceLevelIndicator.CreateCustomerResourceId(serviceId);

        // Assert
        actual.Should().Be($"ServiceTreeId://{serviceId}");
    }

    [Fact]
    public void Cannot_create_CustomerResourceId_with_default_GUID()
    {
        // Arrange
        var serviceId = default(Guid);

        // Act
        var action = () => ServiceLevelIndicator.CreateCustomerResourceId(serviceId);

        // Assert
        action.Should().Throw<ArgumentException>()
            .WithMessage("Value cannot be null. (Parameter 'serviceId')");
    }
}
