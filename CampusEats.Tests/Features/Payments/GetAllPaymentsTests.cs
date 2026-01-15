using CampusEats.Backend.Domain;
using CampusEats.Backend.Features.Payments;
using CampusEats.Tests.Helpers;
using FluentAssertions;

namespace CampusEats.Tests.Features.Payments;

public class GetAllPaymentsTests
{
    [Fact]
    public async Task Handle_ReturnsAllPayments()
    {
        // Arrange
        var context = TestDbContextFactory.CreateInMemoryContext();
        var handler = new GetAllPayments.Handler(context);

        var user = new User { Id = Guid.NewGuid(), Email = "test@campus.ro", PasswordHash = "hash", FullName = "Test", Role = "Student", PhoneNumber = "+40712345678", IsActive = true, CreatedAt = DateTime.UtcNow };
        var order = new Order { Id = Guid.NewGuid(), OrderNumber = "ORD-001", UserId = user.Id, TotalAmount = 100m, Status = "Pending", PaymentStatus = "Paid", CreatedAt = DateTime.UtcNow };

        var payment1 = new Payment { Id = Guid.NewGuid(), OrderId = order.Id, Amount = 100m, Status = PaymentStatus.Completed, Method = PaymentMethod.Card, CreatedAt = DateTime.UtcNow.AddHours(-2) };
        var payment2 = new Payment { Id = Guid.NewGuid(), OrderId = order.Id, Amount = 50m, Status = PaymentStatus.Pending, Method = PaymentMethod.Cash, CreatedAt = DateTime.UtcNow.AddHours(-1) };

        context.Users.Add(user);
        context.Orders.Add(order);
        context.Payments.AddRange(payment1, payment2);
        await context.SaveChangesAsync();

        var query = new GetAllPayments.Query();

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Items.Should().HaveCount(2);
        result.Value.TotalCount.Should().Be(2);
    }

    [Fact]
    public async Task Handle_FiltersByStatus()
    {
        // Arrange
        var context = TestDbContextFactory.CreateInMemoryContext();
        var handler = new GetAllPayments.Handler(context);

        var user = new User { Id = Guid.NewGuid(), Email = "test@campus.ro", PasswordHash = "hash", FullName = "Test", Role = "Student", PhoneNumber = "+40712345678", IsActive = true, CreatedAt = DateTime.UtcNow };
        var order = new Order { Id = Guid.NewGuid(), OrderNumber = "ORD-001", UserId = user.Id, TotalAmount = 100m, Status = "Pending", PaymentStatus = "Paid", CreatedAt = DateTime.UtcNow };

        var payment1 = new Payment { Id = Guid.NewGuid(), OrderId = order.Id, Amount = 100m, Status = PaymentStatus.Completed, Method = PaymentMethod.Card, CreatedAt = DateTime.UtcNow };
        var payment2 = new Payment { Id = Guid.NewGuid(), OrderId = order.Id, Amount = 50m, Status = PaymentStatus.Pending, Method = PaymentMethod.Cash, CreatedAt = DateTime.UtcNow };

        context.Users.Add(user);
        context.Orders.Add(order);
        context.Payments.AddRange(payment1, payment2);
        await context.SaveChangesAsync();

        var query = new GetAllPayments.Query(Status: "Completed");

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.Items.Should().HaveCount(1);
        result.Value.Items.First().Status.Should().Be(PaymentStatus.Completed);
    }

    [Fact]
    public async Task Handle_FiltersByMethod()
    {
        // Arrange
        var context = TestDbContextFactory.CreateInMemoryContext();
        var handler = new GetAllPayments.Handler(context);

        var user = new User { Id = Guid.NewGuid(), Email = "test@campus.ro", PasswordHash = "hash", FullName = "Test", Role = "Student", PhoneNumber = "+40712345678", IsActive = true, CreatedAt = DateTime.UtcNow };
        var order = new Order { Id = Guid.NewGuid(), OrderNumber = "ORD-001", UserId = user.Id, TotalAmount = 100m, Status = "Pending", PaymentStatus = "Paid", CreatedAt = DateTime.UtcNow };

        var payment1 = new Payment { Id = Guid.NewGuid(), OrderId = order.Id, Amount = 100m, Status = PaymentStatus.Completed, Method = PaymentMethod.Card, CreatedAt = DateTime.UtcNow };
        var payment2 = new Payment { Id = Guid.NewGuid(), OrderId = order.Id, Amount = 50m, Status = PaymentStatus.Pending, Method = PaymentMethod.Cash, CreatedAt = DateTime.UtcNow };

        context.Users.Add(user);
        context.Orders.Add(order);
        context.Payments.AddRange(payment1, payment2);
        await context.SaveChangesAsync();

        var query = new GetAllPayments.Query(Method: "Card");

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.Items.Should().HaveCount(1);
        result.Value.Items.First().Method.Should().Be(PaymentMethod.Card);
    }

    [Fact]
    public async Task Handle_Pagination_ReturnsCorrectPage()
    {
        // Arrange
        var context = TestDbContextFactory.CreateInMemoryContext();
        var handler = new GetAllPayments.Handler(context);

        var user = new User { Id = Guid.NewGuid(), Email = "test@campus.ro", PasswordHash = "hash", FullName = "Test", Role = "Student", PhoneNumber = "+40712345678", IsActive = true, CreatedAt = DateTime.UtcNow };
        var order = new Order { Id = Guid.NewGuid(), OrderNumber = "ORD-001", UserId = user.Id, TotalAmount = 100m, Status = "Pending", PaymentStatus = "Paid", CreatedAt = DateTime.UtcNow };

        for (int i = 0; i < 25; i++)
        {
            context.Payments.Add(new Payment
            {
                Id = Guid.NewGuid(),
                OrderId = order.Id,
                Amount = 10m * (i + 1),
                Status = PaymentStatus.Completed,
                Method = PaymentMethod.Card,
                CreatedAt = DateTime.UtcNow.AddMinutes(-i)
            });
        }

        context.Users.Add(user);
        context.Orders.Add(order);
        await context.SaveChangesAsync();

        var query = new GetAllPayments.Query(Page: 2, PageSize: 10);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.Items.Should().HaveCount(10);
        result.Value.CurrentPage.Should().Be(2);
        result.Value.TotalCount.Should().Be(25);
    }

    [Fact]
    public async Task Handle_EmptyDatabase_ReturnsEmptyList()
    {
        // Arrange
        var context = TestDbContextFactory.CreateInMemoryContext();
        var handler = new GetAllPayments.Handler(context);

        var query = new GetAllPayments.Query();

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.Items.Should().BeEmpty();
        result.Value.TotalCount.Should().Be(0);
    }
}
