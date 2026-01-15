using CampusEats.Backend.Domain;
using FluentAssertions;

namespace CampusEats.Tests.Domain;

public class DomainEntityTests
{
    [Fact]
    public void Order_Initialization_SetsDefaultValues()
    {
        // Arrange & Act
        var order = new Order();

        // Assert
        order.Status.Should().Be("Pending");
        order.PaymentStatus.Should().Be("Pending");
        order.OrderItems.Should().NotBeNull();
        order.OrderItems.Should().BeEmpty();
    }

    [Fact]
    public void User_Initialization_SetsDefaultValues()
    {
        // Arrange & Act
        var user = new User();

        // Assert
        user.IsActive.Should().BeTrue();
        user.LoyaltyPoints.Should().Be(0);
        user.Orders.Should().NotBeNull();
        user.Orders.Should().BeEmpty();
        user.LoyaltyTransactions.Should().NotBeNull();
        user.LoyaltyTransactions.Should().BeEmpty();
    }

    [Fact]
    public void Order_Relationships_CanBeSet()
    {
        // Arrange
        var user = new User { Id = Guid.NewGuid(), FullName = "Test User" };
        var order = new Order { Id = Guid.NewGuid(), UserId = user.Id };
        var item = new OrderItem { Id = Guid.NewGuid(), OrderId = order.Id, ProductId = Guid.NewGuid() };

        // Act
        order.User = user;
        order.OrderItems.Add(item);

        // Assert
        order.User.Should().Be(user);
        order.OrderItems.Should().Contain(item);
    }

    [Fact]
    public void Product_Initialization_SetsDefaultValues()
    {
        // Arrange & Act
        var product = new Product();

        // Assert
        product.IsAvailable.Should().BeTrue();
        product.Allergens.Should().NotBeNull();
        product.Allergens.Should().BeEmpty();
    }

    [Fact]
    public void LoyaltyTransaction_Initialization_SetsProperties()
    {
        // Arrange
        var id = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var now = DateTime.UtcNow;

        // Act
        var transaction = new LoyaltyTransaction
        {
            Id = id,
            UserId = userId,
            PointsChange = 50,
            Type = LoyaltyTransactionType.Earned, // Assuming enum or string
            Description = "Test",
            CreatedAt = now
        };

        // Assert
        transaction.Id.Should().Be(id);
        transaction.UserId.Should().Be(userId);
        transaction.PointsChange.Should().Be(50);
        transaction.Type.Should().Be(LoyaltyTransactionType.Earned);
    }
}
