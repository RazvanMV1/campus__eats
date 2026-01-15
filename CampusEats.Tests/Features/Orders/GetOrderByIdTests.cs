using CampusEats.Backend.Domain;
using CampusEats.Backend.Features.Orders;
using CampusEats.Tests.Helpers;
using FluentAssertions;

namespace CampusEats.Tests.Features.Orders;

public class GetOrderByIdTests
{
    [Fact]
    public async Task Handle_ValidOrderId_ReturnsOrder()
    {
        // Arrange
        var context = TestDbContextFactory.CreateInMemoryContext();
        var handler = new GetOrderById.Handler(context);

        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "test@campus.ro",
            PasswordHash = "hash",
            FullName = "Test User",
            Role = "Student",
            PhoneNumber = "+40712345678",
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        var product = new Product
        {
            Id = Guid.NewGuid(),
            Name = "Test Product",
            Description = "Test",
            Price = 25.00m,
            Category = "Main",
            IsAvailable = true,
            CreatedAt = DateTime.UtcNow
        };

        var order = new Order
        {
            Id = Guid.NewGuid(),
            OrderNumber = "ORD-TEST-001",
            UserId = user.Id,
            TotalAmount = 25.00m,
            Status = "Pending",
            PaymentStatus = "Pending",
            CreatedAt = DateTime.UtcNow
        };

        var orderItem = new OrderItem
        {
            Id = Guid.NewGuid(),
            OrderId = order.Id,
            ProductId = product.Id,
            Quantity = 1,
            UnitPrice = 25.00m,
            Subtotal = 25.00m
        };

        context.Users.Add(user);
        context.Products.Add(product);
        context.Orders.Add(order);
        context.OrderItems.Add(orderItem);
        await context.SaveChangesAsync();

        var query = new GetOrderById.Query { OrderId = order.Id, UserId = user.Id };

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.OrderNumber.Should().Be("ORD-TEST-001");
        result.Value.OrderItems.Should().HaveCount(1);
    }

    [Fact]
    public async Task Handle_OrderNotFound_ReturnsFailure()
    {
        // Arrange
        var context = TestDbContextFactory.CreateInMemoryContext();
        var handler = new GetOrderById.Handler(context);

        var query = new GetOrderById.Query { OrderId = Guid.NewGuid(), UserId = Guid.NewGuid() };

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("Order") && e.Contains("not found"));
    }

    [Fact]
    public async Task Handle_UnauthorizedUser_ReturnsFailure()
    {
        // Arrange
        var context = TestDbContextFactory.CreateInMemoryContext();
        var handler = new GetOrderById.Handler(context);

        var user1 = new User { Id = Guid.NewGuid(), Email = "user1@campus.ro", PasswordHash = "hash", FullName = "User 1", Role = "Student", PhoneNumber = "+40712345678", IsActive = true, CreatedAt = DateTime.UtcNow };
        var user2 = new User { Id = Guid.NewGuid(), Email = "user2@campus.ro", PasswordHash = "hash", FullName = "User 2", Role = "Student", PhoneNumber = "+40712345679", IsActive = true, CreatedAt = DateTime.UtcNow };

        var order = new Order
        {
            Id = Guid.NewGuid(),
            OrderNumber = "ORD-001",
            UserId = user1.Id, // Belongs to user1
            TotalAmount = 25.00m,
            Status = "Pending",
            PaymentStatus = "Pending",
            CreatedAt = DateTime.UtcNow
        };

        context.Users.AddRange(user1, user2);
        context.Orders.Add(order);
        await context.SaveChangesAsync();

        var query = new GetOrderById.Query { OrderId = order.Id, UserId = user2.Id }; // user2 trying to access user1's order

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("not authorized"));
    }

    [Fact]
    public async Task Handle_OrderWithMultipleItems_ReturnsAllItems()
    {
        // Arrange
        var context = TestDbContextFactory.CreateInMemoryContext();
        var handler = new GetOrderById.Handler(context);

        var user = new User { Id = Guid.NewGuid(), Email = "test@campus.ro", PasswordHash = "hash", FullName = "Test User", Role = "Student", PhoneNumber = "+40712345678", IsActive = true, CreatedAt = DateTime.UtcNow };
        var product1 = new Product { Id = Guid.NewGuid(), Name = "Burger", Description = "Test", Price = 25m, Category = "Main", IsAvailable = true, CreatedAt = DateTime.UtcNow };
        var product2 = new Product { Id = Guid.NewGuid(), Name = "Fries", Description = "Test", Price = 10m, Category = "Snack", IsAvailable = true, CreatedAt = DateTime.UtcNow };

        var order = new Order { Id = Guid.NewGuid(), OrderNumber = "ORD-001", UserId = user.Id, TotalAmount = 60m, Status = "Pending", PaymentStatus = "Pending", CreatedAt = DateTime.UtcNow };

        var item1 = new OrderItem { Id = Guid.NewGuid(), OrderId = order.Id, ProductId = product1.Id, Quantity = 2, UnitPrice = 25m, Subtotal = 50m };
        var item2 = new OrderItem { Id = Guid.NewGuid(), OrderId = order.Id, ProductId = product2.Id, Quantity = 1, UnitPrice = 10m, Subtotal = 10m };

        context.Users.Add(user);
        context.Products.AddRange(product1, product2);
        context.Orders.Add(order);
        context.OrderItems.AddRange(item1, item2);
        await context.SaveChangesAsync();

        var query = new GetOrderById.Query { OrderId = order.Id, UserId = user.Id };

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.OrderItems.Should().HaveCount(2);
        result.Value.OrderItems.Should().Contain(i => i.ProductName == "Burger" && i.Quantity == 2);
        result.Value.OrderItems.Should().Contain(i => i.ProductName == "Fries" && i.Quantity == 1);
    }
}
