using CampusEats.Backend.Common;
using CampusEats.Backend.Common.Behaviors;
using FluentAssertions;
using FluentValidation;
using FluentValidation.Results;
using MediatR;

namespace CampusEats.Tests.Common.Behaviors;

public class ValidationBehaviorTests
{
    public class TestCommand : IRequest<Result<string>>
    {
        public string Name { get; set; } = string.Empty;
    }

    // Manual mock to avoid Moq optional parameter issues with ValidateAsync
    public class MockValidator : IValidator<TestCommand>
    {
        private readonly ValidationResult _result;

        public MockValidator(ValidationResult result)
        {
            _result = result;
        }

        public Task<ValidationResult> ValidateAsync(IValidationContext context, CancellationToken cancellation = default)
        {
            return Task.FromResult(_result);
        }

        public ValidationResult Validate(IValidationContext context)
        {
            return _result;
        }

        public Task<ValidationResult> ValidateAsync(TestCommand instance, CancellationToken cancellation = default)
        {
            return Task.FromResult(_result);
        }

        public ValidationResult Validate(TestCommand instance)
        {
            return _result;
        }

        public IValidatorDescriptor CreateDescriptor() => throw new NotImplementedException();
        public bool CanValidateInstancesOfType(Type type) => true;
    }

    [Fact]
    public async Task Handle_WithNoValidators_ShouldContinuePipeline()
    {
        // Arrange
        var validators = Enumerable.Empty<IValidator<TestCommand>>();
        var behavior = new ValidationBehavior<TestCommand, Result<string>>(validators);
        var request = new TestCommand { Name = "Test" };
        
        // Act
        // Use a simple lambda for RequestHandlerDelegate - no Moq needed for simply returning a result
        var result = await behavior.Handle(request, (ct) => Task.FromResult(Result<string>.Success("Success")), CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_WithValidRequest_ShouldContinuePipeline()
    {
        // Arrange
        var validator = new MockValidator(new ValidationResult());
        var validators = new List<IValidator<TestCommand>> { validator };
        var behavior = new ValidationBehavior<TestCommand, Result<string>>(validators);
        var request = new TestCommand { Name = "Test" };
        
        // Act
        var result = await behavior.Handle(request, (ct) => Task.FromResult(Result<string>.Success("Success")), CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_WithInvalidRequest_ShouldReturnFailureResult()
    {
        // Arrange
        var failures = new List<ValidationFailure> { new ValidationFailure("Name", "Name is required") };
        var validator = new MockValidator(new ValidationResult(failures));
        var validators = new List<IValidator<TestCommand>> { validator };
        var behavior = new ValidationBehavior<TestCommand, Result<string>>(validators);
        var request = new TestCommand { Name = "" };
        
        // Act
        // For failure case, the delegate should NOT be called, but we still pass one to satisfy the signature
        bool delegateCalled = false;
        var result = await behavior.Handle(request, (ct) => 
        {
            delegateCalled = true;
            return Task.FromResult(Result<string>.Success("Success"));
        }, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Contain("Name is required");
        delegateCalled.Should().BeFalse();
    }
}
