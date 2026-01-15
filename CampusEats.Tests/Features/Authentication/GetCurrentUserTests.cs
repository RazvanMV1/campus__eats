using CampusEats.Backend.Domain;
using CampusEats.Backend.Features.Authentication;
using CampusEats.Tests.Helpers;
using FluentAssertions;

namespace CampusEats.Tests.Features.Authentication;

public class GetCurrentUserTests
{
    [Fact]
    public async Task Handle_ValidUserId_ReturnsUser()
    {
        // Arrange
        var context = TestDbContextFactory.CreateInMemoryContext();
        var handler = new GetCurrentUser.Handler(context);

        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "test@campus.ro",
            PasswordHash = "hash",
            FullName = "Test User",
            Role = "Student",
            PhoneNumber = "+40712345678",
            StudentId = "STU123",
            LoyaltyPoints = 150,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        context.Users.Add(user);
        await context.SaveChangesAsync();

        var query = new GetCurrentUser.Query { UserId = user.Id };

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Id.Should().Be(user.Id);
        result.Value.Email.Should().Be("test@campus.ro");
        result.Value.FullName.Should().Be("Test User");
        result.Value.Role.Should().Be("Student");
        result.Value.LoyaltyPoints.Should().Be(150);
        result.Value.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_InvalidUserId_ReturnsFailure()
    {
        // Arrange
        var context = TestDbContextFactory.CreateInMemoryContext();
        var handler = new GetCurrentUser.Handler(context);

        var query = new GetCurrentUser.Query { UserId = Guid.NewGuid() };

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("User not found"));
    }

    [Fact]
    public async Task Handle_InactiveUser_ReturnsUserWithInactiveStatus()
    {
        // Arrange
        var context = TestDbContextFactory.CreateInMemoryContext();
        var handler = new GetCurrentUser.Handler(context);

        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "inactive@campus.ro",
            PasswordHash = "hash",
            FullName = "Inactive User",
            Role = "Student",
            PhoneNumber = "+40712345678",
            IsActive = false, // Inactive
            CreatedAt = DateTime.UtcNow
        };

        context.Users.Add(user);
        await context.SaveChangesAsync();

        var query = new GetCurrentUser.Query { UserId = user.Id };

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.IsActive.Should().BeFalse();
    }
}