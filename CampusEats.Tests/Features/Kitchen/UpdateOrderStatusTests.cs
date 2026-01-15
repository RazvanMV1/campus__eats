using CampusEats.Backend.Features.Kitchen;
using CampusEats.Backend.Persistence;
using CampusEats.Backend.Domain;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace CampusEats.Backend.Tests.Features.Kitchen;

public class UpdateOrderStatusTests
{
    private static AppDbContext CreateInMemoryContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }

    [Fact]
    public async Task Handle_Should_Update_Order_Status_Successfully()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "test@campus.ro",
            FullName = "Test User",
            Role = "Student",
            PasswordHash = "hash",
            IsActive = true,
            LoyaltyPoints = 0,
            CreatedAt = DateTime.UtcNow
        };

        var product = new Product
        {
            Id = Guid.NewGuid(),
            Name = "Burger",
            Price = 25.00m,
            Category = "Main",
            IsAvailable = true,
            CreatedAt = DateTime.UtcNow
        };

        var order = new Order
        {
            Id = Guid.NewGuid(),
            OrderNumber = "ORD-001",
            UserId = user.Id,
            Status = "Pending",
            PaymentStatus = "Paid",
            TotalAmount = 25.00m,
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

        await context.Users.AddAsync(user);
        await context.Products.AddAsync(product);
        await context.Orders.AddAsync(order);
        await context.OrderItems.AddAsync(orderItem);
        await context.SaveChangesAsync();

        var handler = new UpdateOrderStatus.Handler(context);
        var command = new UpdateOrderStatus.Command 
        { 
            OrderId = order.Id, 
            Status = "Preparing" 
        };

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Status.Should().Be("Preparing");
        
        var updatedOrder = await context.Orders.FindAsync(order.Id);
        updatedOrder!.Status.Should().Be("Preparing");
    }

    [Fact]
    public async Task Handle_Should_Return_Failure_When_Order_Not_Found()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        
        var handler = new UpdateOrderStatus.Handler(context);
        var command = new UpdateOrderStatus.Command 
        { 
            OrderId = Guid.NewGuid(), 
            Status = "Preparing" 
        };

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("not found");
    }

    [Fact]
    public async Task Handle_Should_Set_CompletedAt_When_Status_Is_Completed()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "test@campus.ro",
            FullName = "Test User",
            Role = "Student",
            PasswordHash = "hash",
            IsActive = true,
            LoyaltyPoints = 0,
            CreatedAt = DateTime.UtcNow
        };

        var product = new Product
        {
            Id = Guid.NewGuid(),
            Name = "Burger",
            Price = 25.00m,
            Category = "Main",
            IsAvailable = true,
            CreatedAt = DateTime.UtcNow
        };

        var order = new Order
        {
            Id = Guid.NewGuid(),
            OrderNumber = "ORD-001",
            UserId = user.Id,
            Status = "Ready",
            PaymentStatus = "Paid",
            TotalAmount = 25.00m,
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

        await context.Users.AddAsync(user);
        await context.Products.AddAsync(product);
        await context.Orders.AddAsync(order);
        await context.OrderItems.AddAsync(orderItem);
        await context.SaveChangesAsync();

        var handler = new UpdateOrderStatus.Handler(context);
        var command = new UpdateOrderStatus.Command 
        { 
            OrderId = order.Id, 
            Status = "Completed" 
        };

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        
        var updatedOrder = await context.Orders.FindAsync(order.Id);
        updatedOrder!.Status.Should().Be("Completed");
        updatedOrder.CompletedAt.Should().NotBeNull();
        updatedOrder.CompletedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task Handle_Should_Award_Loyalty_Points_When_Order_Completed()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "test@campus.ro",
            FullName = "Test User",
            Role = "Student",
            PasswordHash = "hash",
            IsActive = true,
            LoyaltyPoints = 0,
            CreatedAt = DateTime.UtcNow
        };

        var product = new Product
        {
            Id = Guid.NewGuid(),
            Name = "Burger",
            Price = 25.00m,
            Category = "Main",
            IsAvailable = true,
            CreatedAt = DateTime.UtcNow
        };

        var order = new Order
        {
            Id = Guid.NewGuid(),
            OrderNumber = "ORD-001",
            UserId = user.Id,
            Status = "Ready",
            PaymentStatus = "Paid",
            TotalAmount = 100.00m,
            CreatedAt = DateTime.UtcNow
        };

        var orderItem = new OrderItem
        {
            Id = Guid.NewGuid(),
            OrderId = order.Id,
            ProductId = product.Id,
            Quantity = 4,
            UnitPrice = 25.00m,
            Subtotal = 100.00m
        };

        await context.Users.AddAsync(user);
        await context.Products.AddAsync(product);
        await context.Orders.AddAsync(order);
        await context.OrderItems.AddAsync(orderItem);
        await context.SaveChangesAsync();

        var handler = new UpdateOrderStatus.Handler(context);
        var command = new UpdateOrderStatus.Command 
        { 
            OrderId = order.Id, 
            Status = "Completed" 
        };

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        
        var updatedUser = await context.Users.FindAsync(user.Id);
        updatedUser!.LoyaltyPoints.Should().Be(10.00m); // 10% of 100 RON
        
        var loyaltyTransaction = await context.LoyaltyTransactions
            .FirstOrDefaultAsync(lt => lt.UserId == user.Id);
        loyaltyTransaction.Should().NotBeNull();
        loyaltyTransaction!.PointsChange.Should().Be(10.00m);
        loyaltyTransaction.Type.Should().Be(LoyaltyTransactionType.Earned);
    }

    [Fact]
    public async Task Handle_Should_Not_Award_Loyalty_Points_Twice_For_Same_Order()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "test@campus.ro",
            FullName = "Test User",
            Role = "Student",
            PasswordHash = "hash",
            IsActive = true,
            LoyaltyPoints = 0,
            CreatedAt = DateTime.UtcNow
        };

        var product = new Product
        {
            Id = Guid.NewGuid(),
            Name = "Burger",
            Price = 25.00m,
            Category = "Main",
            IsAvailable = true,
            CreatedAt = DateTime.UtcNow
        };

        var order = new Order
        {
            Id = Guid.NewGuid(),
            OrderNumber = "ORD-001",
            UserId = user.Id,
            Status = "Completed", // Already completed
            PaymentStatus = "Paid",
            TotalAmount = 100.00m,
            CreatedAt = DateTime.UtcNow,
            CompletedAt = DateTime.UtcNow.AddMinutes(-10)
        };

        var orderItem = new OrderItem
        {
            Id = Guid.NewGuid(),
            OrderId = order.Id,
            ProductId = product.Id,
            Quantity = 4,
            UnitPrice = 25.00m,
            Subtotal = 100.00m
        };

        // User already has points from first completion
        user.LoyaltyPoints = 10.00m;

        await context.Users.AddAsync(user);
        await context.Products.AddAsync(product);
        await context.Orders.AddAsync(order);
        await context.OrderItems.AddAsync(orderItem);
        await context.SaveChangesAsync();

        var handler = new UpdateOrderStatus.Handler(context);
        var command = new UpdateOrderStatus.Command 
        { 
            OrderId = order.Id, 
            Status = "Completed" // Try to complete again
        };

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        
        var updatedUser = await context.Users.FindAsync(user.Id);
        updatedUser!.LoyaltyPoints.Should().Be(10.00m); // Should NOT increase again
    }

    [Fact]
    public async Task Handle_Should_Update_UpdatedAt_Timestamp()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "test@campus.ro",
            FullName = "Test User",
            Role = "Student",
            PasswordHash = "hash",
            IsActive = true,
            LoyaltyPoints = 0,
            CreatedAt = DateTime.UtcNow
        };

        var product = new Product
        {
            Id = Guid.NewGuid(),
            Name = "Burger",
            Price = 25.00m,
            Category = "Main",
            IsAvailable = true,
            CreatedAt = DateTime.UtcNow
        };

        var oldTimestamp = DateTime.UtcNow.AddHours(-1);
        var order = new Order
        {
            Id = Guid.NewGuid(),
            OrderNumber = "ORD-001",
            UserId = user.Id,
            Status = "Pending",
            PaymentStatus = "Paid",
            TotalAmount = 25.00m,
            CreatedAt = oldTimestamp,
            UpdatedAt = oldTimestamp
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

        await context.Users.AddAsync(user);
        await context.Products.AddAsync(product);
        await context.Orders.AddAsync(order);
        await context.OrderItems.AddAsync(orderItem);
        await context.SaveChangesAsync();

        var handler = new UpdateOrderStatus.Handler(context);
        var command = new UpdateOrderStatus.Command 
        { 
            OrderId = order.Id, 
            Status = "Preparing" 
        };

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        
        var updatedOrder = await context.Orders.FindAsync(order.Id);
        updatedOrder!.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        updatedOrder.UpdatedAt.Should().BeAfter(oldTimestamp);
    }

    [Fact]
    public async Task Handle_Should_Include_OrderItems_In_Response()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "test@campus.ro",
            FullName = "Test User",
            Role = "Student",
            PasswordHash = "hash",
            IsActive = true,
            LoyaltyPoints = 0,
            CreatedAt = DateTime.UtcNow
        };

        var product = new Product
        {
            Id = Guid.NewGuid(),
            Name = "Burger",
            Price = 25.00m,
            Category = "Main",
            IsAvailable = true,
            CreatedAt = DateTime.UtcNow
        };

        var order = new Order
        {
            Id = Guid.NewGuid(),
            OrderNumber = "ORD-001",
            UserId = user.Id,
            Status = "Pending",
            PaymentStatus = "Paid",
            TotalAmount = 25.00m,
            CreatedAt = DateTime.UtcNow
        };

        var orderItem = new OrderItem
        {
            Id = Guid.NewGuid(),
            OrderId = order.Id,
            ProductId = product.Id,
            Quantity = 2,
            UnitPrice = 25.00m,
            Subtotal = 50.00m
        };

        await context.Users.AddAsync(user);
        await context.Products.AddAsync(product);
        await context.Orders.AddAsync(order);
        await context.OrderItems.AddAsync(orderItem);
        await context.SaveChangesAsync();

        var handler = new UpdateOrderStatus.Handler(context);
        var command = new UpdateOrderStatus.Command 
        { 
            OrderId = order.Id, 
            Status = "Preparing" 
        };

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.OrderItems.Should().HaveCount(1);
        result.Value.OrderItems.First().ProductName.Should().Be("Burger");
        result.Value.OrderItems.First().Quantity.Should().Be(2);
    }
}