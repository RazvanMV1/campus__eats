using CampusEats.Backend.Domain;
using CampusEats.Backend.Features.Menu;
using CampusEats.Tests.Helpers;
using FluentAssertions;

namespace CampusEats.Tests.Features.Menu;

public class GetProductByIdTests
{
    [Fact]
    public async Task Handle_ValidId_ReturnsProduct()
    {
        // Arrange
        var context = TestDbContextFactory.CreateInMemoryContext();
        var productId = Guid.NewGuid();
        
        var product = new Product
        {
            Id = productId,
            Name = "Test Burger",
            Description = "Delicious burger",
            Price = 25.00m,
            Category = "Main",
            Allergens = new List<string> { "Gluten" },
            IsAvailable = true,
            CreatedAt = DateTime.UtcNow
        };

        context.Products.Add(product);
        await context.SaveChangesAsync();

        var handler = new GetProductById.Handler(context);
        var query = new GetProductById.Query(productId);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Id.Should().Be(productId);
        result.Value.Name.Should().Be("Test Burger");
        result.Value.Price.Should().Be(25.00m);
    }

    [Fact]
    public async Task Handle_NonExistentId_ReturnsFailure()
    {
        // Arrange
        var context = TestDbContextFactory.CreateInMemoryContext();
        var handler = new GetProductById.Handler(context);
        var nonExistentId = Guid.NewGuid();
        var query = new GetProductById.Query(nonExistentId);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("not found");
        result.Error.Should().Contain(nonExistentId.ToString());
    }

    [Fact]
    public async Task Handle_Returns_Unavailable_Product()
    {
        // Arrange
        var context = TestDbContextFactory.CreateInMemoryContext();
        var productId = Guid.NewGuid();
        
        var product = new Product
        {
            Id = productId,
            Name = "Unavailable Product",
            Description = "Test",
            Price = 10.00m,
            Category = "Main",
            IsAvailable = false, // Not available
            CreatedAt = DateTime.UtcNow
        };

        context.Products.Add(product);
        await context.SaveChangesAsync();

        var handler = new GetProductById.Handler(context);
        var query = new GetProductById.Query(productId);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert - GetProductById should return even unavailable products
        result.IsSuccess.Should().BeTrue();
        result.Value!.IsAvailable.Should().BeFalse();
    }

    [Fact]
    public async Task Validator_Rejects_Empty_Id()
    {
        // Arrange
        var validator = new GetProductById.Validator();
        var query = new GetProductById.Query(Guid.Empty);

        // Act
        var result = validator.Validate(query);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Id");
    }
}
