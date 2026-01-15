using CampusEats.Backend.Domain;
using CampusEats.Backend.Features.Menu;
using CampusEats.Tests.Helpers;
using FluentAssertions;

namespace CampusEats.Tests.Features.Menu;

public class GetMenuTests
{
    [Fact]
    public async Task Handle_EmptyDatabase_ReturnsEmptyList()
    {
        // Arrange
        var context = TestDbContextFactory.CreateInMemoryContext();
        var handler = new GetMenu.Handler(context);
        var query = new GetMenu.Query();

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_WithProducts_ReturnsAllAvailableProducts()
    {
        // Arrange
        var context = TestDbContextFactory.CreateInMemoryContext();
        
        var products = new List<Product>
        {
            new()
            {
                Id = Guid.NewGuid(),
                Name = "Burger",
                Description = "Test burger",
                Price = 25.00m,
                Category = "Main",
                Allergens = new List<string> { "Gluten" },
                IsAvailable = true,
                CreatedAt = DateTime.UtcNow
            },
            new()
            {
                Id = Guid.NewGuid(),
                Name = "Pizza",
                Description = "Test pizza",
                Price = 30.00m,
                Category = "Main",
                Allergens = new List<string> { "Gluten", "Lactose" },
                IsAvailable = true,
                CreatedAt = DateTime.UtcNow
            },
            new()
            {
                Id = Guid.NewGuid(),
                Name = "Salad",
                Description = "Test salad",
                Price = 20.00m,
                Category = "Main",
                Allergens = new List<string>(),
                IsAvailable = true,
                CreatedAt = DateTime.UtcNow
            }
        };
        
        context.Products.AddRange(products);
        await context.SaveChangesAsync();
        
        var handler = new GetMenu.Handler(context);
        var query = new GetMenu.Query();

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(3);
        result.Value.Should().Contain(p => p.Name == "Burger");
        result.Value.Should().Contain(p => p.Name == "Pizza");
        result.Value.Should().Contain(p => p.Name == "Salad");
    }

    [Fact]
    public async Task Handle_WithUnavailableProducts_ReturnsOnlyAvailableProducts()
    {
        // Arrange
        var context = TestDbContextFactory.CreateInMemoryContext();
        
        var products = new List<Product>
        {
            new()
            {
                Id = Guid.NewGuid(),
                Name = "Available Product",
                Description = "Test",
                Price = 10.00m,
                Category = "Snack",
                Allergens = new List<string>(),
                IsAvailable = true,
                CreatedAt = DateTime.UtcNow
            },
            new()
            {
                Id = Guid.NewGuid(),
                Name = "Unavailable Product",
                Description = "Test",
                Price = 15.00m,
                Category = "Snack",
                Allergens = new List<string>(),
                IsAvailable = false,
                CreatedAt = DateTime.UtcNow
            }
        };
        
        context.Products.AddRange(products);
        await context.SaveChangesAsync();
        
        var handler = new GetMenu.Handler(context);
        var query = new GetMenu.Query();

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(1);
        result.Value!.First().Name.Should().Be("Available Product");
        result.Value.Should().NotContain(p => p.Name == "Unavailable Product");
    }

    [Fact]
    public async Task Handle_Returns_Products_From_All_Categories()
    {
        // Arrange
        var context = TestDbContextFactory.CreateInMemoryContext();
        
        var products = new[]
        {
            new Product { Id = Guid.NewGuid(), Name = "Burger", Description = "Test", Price = 25m, Category = "Main", IsAvailable = true, CreatedAt = DateTime.UtcNow },
            new Product { Id = Guid.NewGuid(), Name = "Soda", Description = "Test", Price = 5m, Category = "Drink", IsAvailable = true, CreatedAt = DateTime.UtcNow },
            new Product { Id = Guid.NewGuid(), Name = "Ice Cream", Description = "Test", Price = 8m, Category = "Dessert", IsAvailable = true, CreatedAt = DateTime.UtcNow },
            new Product { Id = Guid.NewGuid(), Name = "Chips", Description = "Test", Price = 3m, Category = "Snack", IsAvailable = true, CreatedAt = DateTime.UtcNow }
        };

        context.Products.AddRange(products);
        await context.SaveChangesAsync();

        var handler = new GetMenu.Handler(context);
        var query = new GetMenu.Query();

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(4);
        result.Value.Should().Contain(p => p.Category == "Main");
        result.Value.Should().Contain(p => p.Category == "Drink");
        result.Value.Should().Contain(p => p.Category == "Dessert");
        result.Value.Should().Contain(p => p.Category == "Snack");
    }

    [Fact]
    public async Task Handle_Returns_Complete_Product_Details()
    {
        // Arrange
        var context = TestDbContextFactory.CreateInMemoryContext();
        var productId = Guid.NewGuid();
        
        var product = new Product
        {
            Id = productId,
            Name = "Detailed Product",
            Description = "Full description",
            Price = 19.99m,
            Category = "Main",
            ImageUrl = "https://example.com/image.jpg",
            Allergens = new List<string> { "Gluten", "Dairy" },
            DietaryRestrictions = "Vegetarian",
            IsAvailable = true,
            CreatedAt = DateTime.UtcNow
        };

        context.Products.Add(product);
        await context.SaveChangesAsync();

        var handler = new GetMenu.Handler(context);
        var query = new GetMenu.Query();

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var returnedProduct = result.Value!.First();
        returnedProduct.Id.Should().Be(productId);
        returnedProduct.Name.Should().Be("Detailed Product");
        returnedProduct.Description.Should().Be("Full description");
        returnedProduct.Price.Should().Be(19.99m);
        returnedProduct.ImageUrl.Should().Be("https://example.com/image.jpg");
        returnedProduct.Allergens.Should().Contain("Gluten");
        returnedProduct.DietaryRestrictions.Should().Be("Vegetarian");
    }
}