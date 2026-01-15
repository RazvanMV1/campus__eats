using CampusEats.Backend.Common.DTOs;
using CampusEats.Backend.Domain;
using FluentAssertions;

namespace CampusEats.Tests.Common.DTOs;

public class DtoTests
{
    [Fact]
    public void OrderDto_ShouldMapPropertiesCorrectly()
    {
        var id = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var date = DateTime.UtcNow;

        var dto = new OrderDto
        {
            Id = id,
            OrderNumber = "ORD-001",
            UserId = userId,
            TotalAmount = 50.00m,
            Status = "Pending",
            PaymentStatus = "Unpaid",
            CreatedAt = date,
            OrderItems = new List<OrderItemDto>
            {
                new OrderItemDto { ProductId = Guid.NewGuid(), ProductName = "Burger", Quantity = 1 }
            }
        };

        dto.Id.Should().Be(id);
        dto.OrderNumber.Should().Be("ORD-001");
        dto.UserId.Should().Be(userId);
        dto.TotalAmount.Should().Be(50.00m);
        dto.Status.Should().Be("Pending");
        dto.OrderItems.Should().HaveCount(1);
    }

    [Fact]
    public void ProductDto_ShouldMapPropertiesCorrectly()
    {
        var id = Guid.NewGuid();
        var dto = new ProductDto
        {
            Id = id,
            Name = "Pizza",
            Description = "Delicious",
            Price = 12.50m,
            Category = "Food",
            Allergens = new List<string> { "Gluten" }
        };

        dto.Id.Should().Be(id);
        dto.Name.Should().Be("Pizza");
        dto.Price.Should().Be(12.50m);
        dto.Allergens.Should().Contain("Gluten");
    }

    [Fact]
    public void AuthDto_ShouldMapPropertiesCorrectly()
    {
        var registerDto = new RegisterDto
        {
            Email = "test@test.com",
            Password = "pass",
            FullName = "Test User",
            PhoneNumber = "123456789",
            StudentId = "S123"
        };

        registerDto.Email.Should().Be("test@test.com");
        registerDto.FullName.Should().Be("Test User");
        registerDto.StudentId.Should().Be("S123");

        var loginDto = new LoginDto { Email = "test@test.com", Password = "pass" };
        loginDto.Email.Should().Be("test@test.com");

        var userDto = new UserDto
        {
            Id = Guid.NewGuid(),
            Email = "test@test.com",
            Role = "Student",
            LoyaltyPoints = 100
        };
        userDto.LoyaltyPoints.Should().Be(100);
        userDto.Role.Should().Be("Student");
    }

    [Fact]
    public void PaymentDto_ShouldMapPropertiesCorrectly()
    {
        var dto = new PaymentDto
        {
            Id = Guid.NewGuid(),
            OrderId = Guid.NewGuid(),
            Amount = 25.00m,
            Status = PaymentStatus.Completed,
            Method = PaymentMethod.Card,
            StripePaymentIntentId = "pi_123",
            FailureReason = null
        };

        dto.Amount.Should().Be(25.00m);
        dto.Status.Should().Be(PaymentStatus.Completed);
        dto.Method.Should().Be(PaymentMethod.Card);
        dto.StripePaymentIntentId.Should().Be("pi_123");
        dto.FailureReason.Should().BeNull();
    }

    [Fact]
    public void InventoryReportDto_ShouldMapPropertiesCorrectly()
    {
        var dto = new InventoryReportDto
        {
            ReportDate = DateTime.Today,
            TotalRevenue = 1000m,
            TotalItemsSold = 50,
            InventoryItems = new List<InventoryItemDto>
            {
                new InventoryItemDto { ProductName = "Coke", QuantitySold = 10, Revenue = 20m }
            }
        };

        dto.TotalRevenue.Should().Be(1000m);
        dto.TotalItemsSold.Should().Be(50);
        dto.InventoryItems.First().ProductName.Should().Be("Coke");
    }

    [Fact]
    public void LoyaltyDto_ShouldMapPropertiesCorrectly()
    {
        var pointsDto = new LoyaltyPointsDto
        {
            UserId = Guid.NewGuid(),
            CurrentPoints = 500,
            PointsValue = 5.00m
        };
        pointsDto.CurrentPoints.Should().Be(500);

        var transactionDto = new LoyaltyTransactionDto
        {
            PointsChange = 10,
            Type = "Earned",
            Description = "Order reward"
        };
        transactionDto.PointsChange.Should().Be(10);
        transactionDto.Description.Should().Be("Order reward");
    }

    [Fact]
    public void StripeSettings_ShouldAllowSetGet()
    {
        var s = new CampusEats.Backend.Common.Config.StripeSettings
        {
            SecretKey = "sk_test_123",
            PublicKey = "pk_test_123",
            WebhookSecret = "whsec_123"
        };

        s.SecretKey.Should().Be("sk_test_123");
        s.PublicKey.Should().Be("pk_test_123");
        s.WebhookSecret.Should().Be("whsec_123");
    }
}
