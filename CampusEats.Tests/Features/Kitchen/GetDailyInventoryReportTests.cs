using CampusEats.Backend.Features.Kitchen;
using CampusEats.Backend.Persistence;
using CampusEats.Backend.Domain;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace CampusEats.Backend.Tests.Features.Kitchen;

public class GetDailyInventoryReportTests
{
    private static AppDbContext CreateInMemoryContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }

    [Fact]
    public async Task Handle_Should_Return_Report_With_Completed_Orders()
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
            CreatedAt = DateTime.UtcNow
        };

        var product1 = new Product
        {
            Id = Guid.NewGuid(),
            Name = "Burger",
            Price = 25.00m,
            Category = "Main",
            IsAvailable = true,
            CreatedAt = DateTime.UtcNow
        };

        var product2 = new Product
        {
            Id = Guid.NewGuid(),
            Name = "Pizza",
            Price = 30.00m,
            Category = "Main",
            IsAvailable = true,
            CreatedAt = DateTime.UtcNow
        };

        var reportDate = DateTime.UtcNow.Date;
        
        var completedOrder = new Order
        {
            Id = Guid.NewGuid(),
            OrderNumber = "ORD-001",
            UserId = user.Id,
            Status = "Completed",
            PaymentStatus = "Paid",
            TotalAmount = 55.00m,
            CreatedAt = reportDate.AddHours(10),
            CompletedAt = reportDate.AddHours(11)
        };

        var orderItem1 = new OrderItem
        {
            Id = Guid.NewGuid(),
            OrderId = completedOrder.Id,
            ProductId = product1.Id,
            Quantity = 2,
            UnitPrice = 25.00m,
            Subtotal = 50.00m
        };

        var orderItem2 = new OrderItem
        {
            Id = Guid.NewGuid(),
            OrderId = completedOrder.Id,
            ProductId = product2.Id,
            Quantity = 1,
            UnitPrice = 30.00m,
            Subtotal = 30.00m
        };

        await context.Users.AddAsync(user);
        await context.Products.AddRangeAsync(product1, product2);
        await context.Orders.AddAsync(completedOrder);
        await context.OrderItems.AddRangeAsync(orderItem1, orderItem2);
        await context.SaveChangesAsync();

        var handler = new GetDailyInventoryReport.Handler(context);
        var query = new GetDailyInventoryReport.Query { ReportDate = reportDate };

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.ReportDate.Should().Be(reportDate);
        result.Value.TotalRevenue.Should().Be(55.00m);
        result.Value.TotalOrdersProcessed.Should().Be(1);
        result.Value.TotalItemsSold.Should().Be(3);
        result.Value.InventoryItems.Should().HaveCount(2);
        result.Value.InventoryItems.First().ProductName.Should().Be("Burger");
        result.Value.InventoryItems.First().QuantitySold.Should().Be(2);
    }

    [Fact]
    public async Task Handle_Should_Return_Empty_Report_When_No_Completed_Orders()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        
        var reportDate = DateTime.UtcNow.Date;
        
        var handler = new GetDailyInventoryReport.Handler(context);
        var query = new GetDailyInventoryReport.Query { ReportDate = reportDate };

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.TotalRevenue.Should().Be(0);
        result.Value.TotalOrdersProcessed.Should().Be(0);
        result.Value.TotalItemsSold.Should().Be(0);
        result.Value.InventoryItems.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_Should_Only_Include_Orders_From_Specified_Date()
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

        var reportDate = DateTime.UtcNow.Date;
        var yesterdayDate = reportDate.AddDays(-1);
        
        // Order completed today
        var todayOrder = new Order
        {
            Id = Guid.NewGuid(),
            OrderNumber = "ORD-TODAY",
            UserId = user.Id,
            Status = "Completed",
            PaymentStatus = "Paid",
            TotalAmount = 25.00m,
            CreatedAt = reportDate.AddHours(10),
            CompletedAt = reportDate.AddHours(11)
        };

        // Order completed yesterday
        var yesterdayOrder = new Order
        {
            Id = Guid.NewGuid(),
            OrderNumber = "ORD-YESTERDAY",
            UserId = user.Id,
            Status = "Completed",
            PaymentStatus = "Paid",
            TotalAmount = 25.00m,
            CreatedAt = yesterdayDate.AddHours(10),
            CompletedAt = yesterdayDate.AddHours(11)
        };

        var todayOrderItem = new OrderItem
        {
            Id = Guid.NewGuid(),
            OrderId = todayOrder.Id,
            ProductId = product.Id,
            Quantity = 1,
            UnitPrice = 25.00m,
            Subtotal = 25.00m
        };

        var yesterdayOrderItem = new OrderItem
        {
            Id = Guid.NewGuid(),
            OrderId = yesterdayOrder.Id,
            ProductId = product.Id,
            Quantity = 1,
            UnitPrice = 25.00m,
            Subtotal = 25.00m
        };

        await context.Users.AddAsync(user);
        await context.Products.AddAsync(product);
        await context.Orders.AddRangeAsync(todayOrder, yesterdayOrder);
        await context.OrderItems.AddRangeAsync(todayOrderItem, yesterdayOrderItem);
        await context.SaveChangesAsync();

        var handler = new GetDailyInventoryReport.Handler(context);
        var query = new GetDailyInventoryReport.Query { ReportDate = reportDate };

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.TotalOrdersProcessed.Should().Be(1);
        result.Value.TotalRevenue.Should().Be(25.00m);
        result.Value.TotalItemsSold.Should().Be(1);
    }

    [Fact]
    public async Task Handle_Should_Exclude_Pending_Orders()
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

        var reportDate = DateTime.UtcNow.Date;
        
        var pendingOrder = new Order
        {
            Id = Guid.NewGuid(),
            OrderNumber = "ORD-PENDING",
            UserId = user.Id,
            Status = "Pending",
            PaymentStatus = "Paid",
            TotalAmount = 25.00m,
            CreatedAt = reportDate.AddHours(10)
        };

        var orderItem = new OrderItem
        {
            Id = Guid.NewGuid(),
            OrderId = pendingOrder.Id,
            ProductId = product.Id,
            Quantity = 1,
            UnitPrice = 25.00m,
            Subtotal = 25.00m
        };

        await context.Users.AddAsync(user);
        await context.Products.AddAsync(product);
        await context.Orders.AddAsync(pendingOrder);
        await context.OrderItems.AddAsync(orderItem);
        await context.SaveChangesAsync();

        var handler = new GetDailyInventoryReport.Handler(context);
        var query = new GetDailyInventoryReport.Query { ReportDate = reportDate };

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.TotalOrdersProcessed.Should().Be(0);
        result.Value.TotalRevenue.Should().Be(0);
        result.Value.InventoryItems.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_Should_Use_Current_Date_When_ReportDate_Is_Null()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        
        var handler = new GetDailyInventoryReport.Handler(context);
        var query = new GetDailyInventoryReport.Query { ReportDate = null };

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.ReportDate.Should().Be(DateTime.UtcNow.Date);
    }

    [Fact]
    public async Task Handle_Should_Aggregate_Multiple_Orders_For_Same_Product()
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

        var reportDate = DateTime.UtcNow.Date;
        
        var order1 = new Order
        {
            Id = Guid.NewGuid(),
            OrderNumber = "ORD-001",
            UserId = user.Id,
            Status = "Completed",
            PaymentStatus = "Paid",
            TotalAmount = 50.00m,
            CreatedAt = reportDate.AddHours(10),
            CompletedAt = reportDate.AddHours(11)
        };

        var order2 = new Order
        {
            Id = Guid.NewGuid(),
            OrderNumber = "ORD-002",
            UserId = user.Id,
            Status = "Completed",
            PaymentStatus = "Paid",
            TotalAmount = 25.00m,
            CreatedAt = reportDate.AddHours(14),
            CompletedAt = reportDate.AddHours(15)
        };

        var orderItem1 = new OrderItem
        {
            Id = Guid.NewGuid(),
            OrderId = order1.Id,
            ProductId = product.Id,
            Quantity = 2,
            UnitPrice = 25.00m,
            Subtotal = 50.00m
        };

        var orderItem2 = new OrderItem
        {
            Id = Guid.NewGuid(),
            OrderId = order2.Id,
            ProductId = product.Id,
            Quantity = 1,
            UnitPrice = 25.00m,
            Subtotal = 25.00m
        };

        await context.Users.AddAsync(user);
        await context.Products.AddAsync(product);
        await context.Orders.AddRangeAsync(order1, order2);
        await context.OrderItems.AddRangeAsync(orderItem1, orderItem2);
        await context.SaveChangesAsync();

        var handler = new GetDailyInventoryReport.Handler(context);
        var query = new GetDailyInventoryReport.Query { ReportDate = reportDate };

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.TotalOrdersProcessed.Should().Be(2);
        result.Value.TotalRevenue.Should().Be(75.00m);
        result.Value.TotalItemsSold.Should().Be(3);
        result.Value.InventoryItems.Should().HaveCount(1);
        result.Value.InventoryItems.First().QuantitySold.Should().Be(3);
        result.Value.InventoryItems.First().Revenue.Should().Be(75.00m);
    }
}