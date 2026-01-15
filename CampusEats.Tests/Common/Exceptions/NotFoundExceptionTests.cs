using CampusEats.Backend.Common.Exceptions;
using FluentAssertions;

namespace CampusEats.Tests.Common.Exceptions;

public class NotFoundExceptionTests
{
    [Fact]
    public void Constructor_WithMessage_ShouldSetMessage()
    {
        // Arrange
        var message = "Specific error message";

        // Act
        var exception = new NotFoundException(message);

        // Assert
        exception.Message.Should().Be(message);
    }

    [Fact]
    public void Constructor_WithEntityAndKey_ShouldFormatMessageCorrectly()
    {
        // Arrange
        var entityName = "Order";
        var key = 123;

        // Act
        var exception = new NotFoundException(entityName, key);

        // Assert
        exception.Message.Should().Be($"Order with id '123' was not found.");
    }
}
