namespace ServiceLevelIndicators.Tests;

using FluentAssertions;
using Xunit;

public class LocationIdTests
{
    [Fact]
    public void Will_create_LocationId_with_cloud()
    {
        // Arrange
        // Act
        var actual = ServiceLevelIndicator.CreateLocationId("Public");

        // Assert
        actual.Should().Be("ms-loc://az/Public");
    }

    [Fact]
    public void Will_create_LocationId_with_cloud_region()
    {
        // Arrange
        // Act
        var actual = ServiceLevelIndicator.CreateLocationId("Public", "eastus2");

        // Assert
        actual.Should().Be("ms-loc://az/Public/eastus2");
    }

    [Fact]
    public void Will_create_LocationId_with_cloud_region_zone()
    {
        // Arrange
        // Act
        var actual = ServiceLevelIndicator.CreateLocationId("Public", "eastus2", "1");

        // Assert
        actual.Should().Be("ms-loc://az/Public/eastus2/1");
    }
}
