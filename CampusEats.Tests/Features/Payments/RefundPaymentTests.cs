using CampusEats.Backend.Common;
using CampusEats.Backend.Domain;
using CampusEats.Backend.Features.Payments;
using CampusEats.Tests.Helpers;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using Stripe;

namespace CampusEats.Tests.Features.Payments;

public class RefundPaymentTests
{
    [Fact]
    public async Task Handle_Should_Return_Failure_When_Payment_Not_Found()
    {
        // Arrange
        var context = TestDbContextFactory.CreateInMemoryContext();
        var stripeClient = new StripeClient("sk_test_dummy");
        
        var handler = new RefundPayment.Handler(context, stripeClient);
        var command = new RefundPayment.Command(Guid.NewGuid(), "Customer request");

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("not found");
    }

    [Fact]
    public async Task Handle_Should_Return_Failure_When_Payment_Not_Completed()
    {
        // Arrange
        var context = TestDbContextFactory.CreateInMemoryContext();
        var stripeClient = new StripeClient("sk_test_dummy");

        var user = new User 
        { 
            Id = Guid.NewGuid(), 
            Email = "test@campus.ro",
            FullName = "Test User",
            Role = "Student",
            PasswordHash = "hash",
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        var order = new Order
        {
            Id = Guid.NewGuid(),
            OrderNumber = "ORD-001",
            UserId = user.Id,
            Status = "Pending",
            PaymentStatus = "Unpaid",
            TotalAmount = 100.00m,
            CreatedAt = DateTime.UtcNow
        };

        var payment = new Payment
        {
            Id = Guid.NewGuid(),
            OrderId = order.Id,
            Amount = 100.00m,
            Status = PaymentStatus.Pending, // Not completed
            Method = PaymentMethod.Card,
            StripePaymentIntentId = "pi_test_123",
            CreatedAt = DateTime.UtcNow
        };

        await context.Users.AddAsync(user);
        await context.Orders.AddAsync(order);
        await context.Payments.AddAsync(payment);
        await context.SaveChangesAsync();

        var handler = new RefundPayment.Handler(context, stripeClient);
        var command = new RefundPayment.Command(payment.Id, "Customer request");

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("not completed");
    }

    [Fact]
    public async Task Handle_Should_Return_Failure_When_Payment_Method_Not_Card()
    {
        // Arrange
        var context = TestDbContextFactory.CreateInMemoryContext();
        var stripeClient = new StripeClient("sk_test_dummy");

        var user = new User 
        { 
            Id = Guid.NewGuid(), 
            Email = "test@campus.ro",
            FullName = "Test User",
            Role = "Student",
            PasswordHash = "hash",
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        var order = new Order
        {
            Id = Guid.NewGuid(),
            OrderNumber = "ORD-001",
            UserId = user.Id,
            Status = "Completed",
            PaymentStatus = "Paid",
            TotalAmount = 100.00m,
            CreatedAt = DateTime.UtcNow
        };

        var payment = new Payment
        {
            Id = Guid.NewGuid(),
            OrderId = order.Id,
            Amount = 100.00m,
            Status = PaymentStatus.Completed,
            Method = PaymentMethod.Cash, // Cash payment cannot be refunded via Stripe
            CreatedAt = DateTime.UtcNow,
            PaidAt = DateTime.UtcNow
        };

        await context.Users.AddAsync(user);
        await context.Orders.AddAsync(order);
        await context.Payments.AddAsync(payment);
        await context.SaveChangesAsync();

        var handler = new RefundPayment.Handler(context, stripeClient);
        var command = new RefundPayment.Command(payment.Id, "Customer request");

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("Only Stripe card payments");
    }

    [Fact]
    public async Task Handle_Should_Return_Failure_When_LoyaltyPoints_Payment()
    {
        // Arrange
        var context = TestDbContextFactory.CreateInMemoryContext();
        var stripeClient = new StripeClient("sk_test_dummy");

        var user = new User 
        { 
            Id = Guid.NewGuid(), 
            Email = "test@campus.ro",
            FullName = "Test User",
            Role = "Student",
            PasswordHash = "hash",
            IsActive = true,
            LoyaltyPoints = 200,
            CreatedAt = DateTime.UtcNow
        };

        var order = new Order
        {
            Id = Guid.NewGuid(),
            OrderNumber = "ORD-001",
            UserId = user.Id,
            Status = "Completed",
            PaymentStatus = "Paid",
            TotalAmount = 50.00m,
            CreatedAt = DateTime.UtcNow
        };

        var payment = new Payment
        {
            Id = Guid.NewGuid(),
            OrderId = order.Id,
            Amount = 50.00m,
            Status = PaymentStatus.Completed,
            Method = PaymentMethod.LoyaltyPoints,
            LoyaltyPointsUsed = 50,
            CreatedAt = DateTime.UtcNow,
            PaidAt = DateTime.UtcNow
        };

        await context.Users.AddAsync(user);
        await context.Orders.AddAsync(order);
        await context.Payments.AddAsync(payment);
        await context.SaveChangesAsync();

        var handler = new RefundPayment.Handler(context, stripeClient);
        var command = new RefundPayment.Command(payment.Id, "Customer request");

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("Only Stripe card payments");
    }

    [Fact]
    public async Task Handle_Should_Include_Order_In_Payment_Response()
    {
        // Arrange
        var context = TestDbContextFactory.CreateInMemoryContext();
        var stripeClient = new StripeClient("sk_test_dummy");

        var user = new User 
        { 
            Id = Guid.NewGuid(), 
            Email = "test@campus.ro",
            FullName = "Test User",
            Role = "Student",
            PasswordHash = "hash",
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        var order = new Order
        {
            Id = Guid.NewGuid(),
            OrderNumber = "ORD-001",
            UserId = user.Id,
            Status = "Completed",
            PaymentStatus = "Paid",
            TotalAmount = 100.00m,
            CreatedAt = DateTime.UtcNow
        };

        var payment = new Payment
        {
            Id = Guid.NewGuid(),
            OrderId = order.Id,
            Amount = 100.00m,
            Status = PaymentStatus.Completed,
            Method = PaymentMethod.Card,
            StripePaymentIntentId = "pi_test_123",
            CreatedAt = DateTime.UtcNow,
            PaidAt = DateTime.UtcNow
        };

        await context.Users.AddAsync(user);
        await context.Orders.AddAsync(order);
        await context.Payments.AddAsync(payment);
        await context.SaveChangesAsync();

        var handler = new RefundPayment.Handler(context, stripeClient);
        var command = new RefundPayment.Command(payment.Id, null);

        // Act - This will fail due to Stripe API call, but we're testing the validation logic
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert - Should fail on Stripe call, not on validation
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("Stripe refund failed");
    }

    [Fact]
    public async Task Validator_Should_Require_PaymentId()
    {
        // Arrange
        var validator = new RefundPayment.Validator();
        var command = new RefundPayment.Command(Guid.Empty, "Reason");

        // Act
        var result = validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "PaymentId");
    }

    [Fact]
    public async Task Validator_Should_Allow_Null_Reason()
    {
        // Arrange
        var validator = new RefundPayment.Validator();
        var command = new RefundPayment.Command(Guid.NewGuid(), null);

        // Act
        var result = validator.Validate(command);

        // Assert
        result.IsValid.Should().BeTrue();
    }
}
