using CampusEats.Backend.Domain;
using CampusEats.Backend.Features.Payments;
using CampusEats.Tests.Helpers;
using FluentAssertions;

namespace CampusEats.Tests.Features.Payments;

public class GetOrderPaymentsTests
{
    [Fact]
    public async Task Handle_ReturnsPaymentsForOrder()
    {
        // Arrange
        var context = TestDbContextFactory.CreateInMemoryContext();
        var handler = new GetOrderPayments.Handler(context);

        var user = new User { Id = Guid.NewGuid(), Email = "test@campus.ro", PasswordHash = "hash", FullName = "Test", Role = "Student", PhoneNumber = "+40712345678", IsActive = true, CreatedAt = DateTime.UtcNow };
        var order = new Order { Id = Guid.NewGuid(), OrderNumber = "ORD-001", UserId = user.Id, TotalAmount = 100m, Status = "Pending", PaymentStatus = "Paid", CreatedAt = DateTime.UtcNow };

        var payment1 = new Payment { Id = Guid.NewGuid(), OrderId = order.Id, Amount = 100m, Status = PaymentStatus.Completed, Method = PaymentMethod.Card, CreatedAt = DateTime.UtcNow.AddHours(-2) };
        var payment2 = new Payment { Id = Guid.NewGuid(), OrderId = order.Id, Amount = 50m, Status = PaymentStatus.Failed, Method = PaymentMethod.Card, CreatedAt = DateTime.UtcNow.AddHours(-1) };

        context.Users.Add(user);
        context.Orders.Add(order);
        context.Payments.AddRange(payment1, payment2);
        await context.SaveChangesAsync();

        var query = new GetOrderPayments.Query(order.Id);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().HaveCount(2);
        result.Should().Contain(p => p.Status == PaymentStatus.Completed);
        result.Should().Contain(p => p.Status == PaymentStatus.Failed);
    }

    [Fact]
    public async Task Handle_OrderWithNoPayments_ReturnsEmptyList()
    {
        // Arrange
        var context = TestDbContextFactory.CreateInMemoryContext();
        var handler = new GetOrderPayments.Handler(context);

        var user = new User { Id = Guid.NewGuid(), Email = "test@campus.ro", PasswordHash = "hash", FullName = "Test", Role = "Student", PhoneNumber = "+40712345678", IsActive = true, CreatedAt = DateTime.UtcNow };
        var order = new Order { Id = Guid.NewGuid(), OrderNumber = "ORD-001", UserId = user.Id, TotalAmount = 100m, Status = "Pending", PaymentStatus = "Unpaid", CreatedAt = DateTime.UtcNow };

        context.Users.Add(user);
        context.Orders.Add(order);
        await context.SaveChangesAsync();

        var query = new GetOrderPayments.Query(order.Id);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_NonExistentOrder_ReturnsEmptyList()
    {
        // Arrange
        var context = TestDbContextFactory.CreateInMemoryContext();
        var handler = new GetOrderPayments.Handler(context);

        var query = new GetOrderPayments.Query(Guid.NewGuid());

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_OrdersPayments_DescendingByCreatedAt()
    {
        // Arrange
        var context = TestDbContextFactory.CreateInMemoryContext();
        var handler = new GetOrderPayments.Handler(context);

        var user = new User { Id = Guid.NewGuid(), Email = "test@campus.ro", PasswordHash = "hash", FullName = "Test", Role = "Student", PhoneNumber = "+40712345678", IsActive = true, CreatedAt = DateTime.UtcNow };
        var order = new Order { Id = Guid.NewGuid(), OrderNumber = "ORD-001", UserId = user.Id, TotalAmount = 100m, Status = "Pending", PaymentStatus = "Paid", CreatedAt = DateTime.UtcNow };

        var payment1 = new Payment { Id = Guid.NewGuid(), OrderId = order.Id, Amount = 100m, Status = PaymentStatus.Completed, Method = PaymentMethod.Card, CreatedAt = DateTime.UtcNow.AddHours(-3) };
        var payment2 = new Payment { Id = Guid.NewGuid(), OrderId = order.Id, Amount = 50m, Status = PaymentStatus.Failed, Method = PaymentMethod.Card, CreatedAt = DateTime.UtcNow.AddHours(-1) };
        var payment3 = new Payment { Id = Guid.NewGuid(), OrderId = order.Id, Amount = 75m, Status = PaymentStatus.Completed, Method = PaymentMethod.Card, CreatedAt = DateTime.UtcNow };

        context.Users.Add(user);
        context.Orders.Add(order);
        context.Payments.AddRange(payment1, payment2, payment3);
        await context.SaveChangesAsync();

        var query = new GetOrderPayments.Query(order.Id);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().HaveCount(3);
        result[0].Amount.Should().Be(75m); // Latest
        result[1].Amount.Should().Be(50m);
        result[2].Amount.Should().Be(100m); // Oldest
    }
}
