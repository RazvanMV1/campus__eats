using CampusEats.Backend.Common;
using FluentAssertions;

namespace CampusEats.Tests.Common;

public class ResultTests
{
    [Fact]
    public void Success_CreatesSuccessfulResult()
    {
        // Act
        var result = Result<int>.Success(42);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(42);
        result.Error.Should().BeNull();
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public void Failure_WithSingleError_CreatesFailedResult()
    {
        // Act
        var result = Result<int>.Failure("Something went wrong");

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Value.Should().Be(0);
        result.Error.Should().Be("Something went wrong");
        result.Errors.Should().ContainSingle();
        result.Errors.Should().Contain("Something went wrong");
    }

    [Fact]
    public void Failure_WithMultipleErrors_CreatesFailedResult()
    {
        // Arrange
        var errors = new List<string> { "Error 1", "Error 2", "Error 3" };

        // Act
        var result = Result<int>.Failure(errors);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Value.Should().Be(0);
        result.Error.Should().Be("Error 1"); // First error
        result.Errors.Should().HaveCount(3);
        result.Errors.Should().Contain("Error 1");
        result.Errors.Should().Contain("Error 2");
        result.Errors.Should().Contain("Error 3");
    }

    [Fact]
    public void ImplicitBoolConversion_Success_ReturnsTrue()
    {
        // Arrange
        var result = Result<string>.Success("test");

        // Act
        bool isSuccess = result;

        // Assert
        isSuccess.Should().BeTrue();
    }

    [Fact]
    public void ImplicitBoolConversion_Failure_ReturnsFalse()
    {
        // Arrange
        var result = Result<string>.Failure("error");

        // Act
        bool isSuccess = result;

        // Assert
        isSuccess.Should().BeFalse();
    }

    [Fact]
    public void NonGenericResult_Success_CreatesSuccessfulResult()
    {
        // Act
        var result = Result.Success();

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Error.Should().BeNull();
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public void NonGenericResult_Failure_WithSingleError()
    {
        // Act
        var result = Result.Failure("Operation failed");

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("Operation failed");
        result.Errors.Should().ContainSingle();
        result.Errors.Should().Contain("Operation failed");
    }

    [Fact]
    public void NonGenericResult_Failure_WithMultipleErrors()
    {
        // Arrange
        var errors = new List<string> { "Validation error 1", "Validation error 2" };

        // Act
        var result = Result.Failure(errors);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("Validation error 1");
        result.Errors.Should().HaveCount(2);
    }

    [Fact]
    public void NonGenericResult_ImplicitBoolConversion()
    {
        // Arrange
        var successResult = Result.Success();
        var failureResult = Result.Failure("error");

        // Act & Assert
        ((bool)successResult).Should().BeTrue();
        ((bool)failureResult).Should().BeFalse();
    }
}
