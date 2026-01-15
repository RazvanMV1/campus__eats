using CampusEats.Backend.Domain;
using CampusEats.Backend.Features.Orders;
using CampusEats.Tests.Helpers;
using FluentAssertions;

namespace CampusEats.Tests.Features.Orders;

public class GetUserOrdersTests
{
    [Fact]
    public async Task Handle_ValidUserId_ReturnsUserOrders()
    {
        // Arrange
        var context = TestDbContextFactory.CreateInMemoryContext();
        var handler = new GetUserOrders.Handler(context);

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
            Allergens = new List<string>(),
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

        var query = new GetUserOrders.Query { UserId = user.Id };

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(1);
        result.Value![0].OrderNumber.Should().Be("ORD-TEST-001");
        result.Value[0].OrderItems.Should().HaveCount(1);
    }

    [Fact]
    public async Task Handle_InvalidUserId_ReturnsFailure()
    {
        // Arrange
        var context = TestDbContextFactory.CreateInMemoryContext();
        var handler = new GetUserOrders.Handler(context);

        var query = new GetUserOrders.Query { UserId = Guid.NewGuid() };

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("User") && e.Contains("not found"));
    }

    [Fact]
    public async Task Handle_UserWithNoOrders_ReturnsEmptyList()
    {
        // Arrange
        var context = TestDbContextFactory.CreateInMemoryContext();
        var handler = new GetUserOrders.Handler(context);

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

        context.Users.Add(user);
        await context.SaveChangesAsync();

        var query = new GetUserOrders.Query { UserId = user.Id };

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_MultipleOrders_ReturnsAllOrders()
    {
        // Arrange
        var context = TestDbContextFactory.CreateInMemoryContext();
        var handler = new GetUserOrders.Handler(context);

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

        var order1 = new Order { Id = Guid.NewGuid(), OrderNumber = "ORD-001", UserId = user.Id, TotalAmount = 25m, Status = "Pending", PaymentStatus = "Pending", CreatedAt = DateTime.UtcNow.AddHours(-2) };
        var order2 = new Order { Id = Guid.NewGuid(), OrderNumber = "ORD-002", UserId = user.Id, TotalAmount = 30m, Status = "Completed", PaymentStatus = "Paid", CreatedAt = DateTime.UtcNow.AddHours(-1) };
        var order3 = new Order { Id = Guid.NewGuid(), OrderNumber = "ORD-003", UserId = user.Id, TotalAmount = 15m, Status = "Preparing", PaymentStatus = "Paid", CreatedAt = DateTime.UtcNow };

        context.Users.Add(user);
        context.Products.Add(product);
        context.Orders.AddRange(order1, order2, order3);
        await context.SaveChangesAsync();

        var query = new GetUserOrders.Query { UserId = user.Id };

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(3);
        result.Value![0].OrderNumber.Should().Be("ORD-003"); // Latest first
        result.Value[1].OrderNumber.Should().Be("ORD-002");
        result.Value[2].OrderNumber.Should().Be("ORD-001");
    }

    [Fact]
    public async Task Handle_OrdersWithItems_IncludesAllOrderItems()
    {
        // Arrange
        var context = TestDbContextFactory.CreateInMemoryContext();
        var handler = new GetUserOrders.Handler(context);

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

        var query = new GetUserOrders.Query { UserId = user.Id };

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value![0].OrderItems.Should().HaveCount(2);
        result.Value[0].OrderItems.Should().Contain(i => i.ProductName == "Burger");
        result.Value[0].OrderItems.Should().Contain(i => i.ProductName == "Fries");
    }
}