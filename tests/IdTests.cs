namespace ServiceLevelIndicators.Tests
{
    using FluentAssertions;
    using Xunit;

    public class IdTests
    {
        [Fact]
        public void Will_create_CustomerResourceId()
        {
            // Arrange
            // Act
            var actual = ServiceLevelIndicator.CreateCustomerResourceId("myproduct", "myservice");

            // Assert
            actual.Should().Be("myproduct_myservice");
        }

        [Fact]
        public void Will_create_LocationId()
        {
            // Arrange
            // Act
            var actual = ServiceLevelIndicator.CreateLocationId("public", "west");

            // Assert
            actual.Should().Be("public_west");
        }

        [Fact]
        public void Will_create_LocationId_with_stamp()
        {
            // Arrange
            // Act
            var actual = ServiceLevelIndicator.CreateLocationId("gov", "west", "stamp");

            // Assert
            actual.Should().Be("gov_west_stamp");
        }
    }
}
