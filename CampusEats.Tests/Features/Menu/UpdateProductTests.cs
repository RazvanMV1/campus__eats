using CampusEats.Backend.Domain;
using CampusEats.Backend.Features.Menu;
using CampusEats.Tests.Helpers;
using FluentAssertions;

namespace CampusEats.Tests.Features.Menu;

public class UpdateProductTests
{
    [Fact]
    public async Task Handle_ValidCommand_ReturnsSuccessWithUpdatedProduct()
    {
        // Arrange
        var context = TestDbContextFactory.CreateInMemoryContext();
        var productId = Guid.NewGuid();
        
        // Create initial product
        var product = new Product
        {
            Id = productId,
            Name = "Original Name",
            Description = "Original Description",
            Price = 20.00m,
            Category = "Main",
            Allergens = new List<string> { "Gluten" },
            IsAvailable = true,
            CreatedAt = DateTime.UtcNow
        };
        context.Products.Add(product);
        await context.SaveChangesAsync();
        
        var handler = new UpdateProduct.Handler(context);
        var command = new UpdateProduct.Command
        {
            Id = productId,
            Name = "Updated Name",
            Description = "Updated Description",
            Price = 25.00m,
            Category = "Dessert",
            Allergens = new List<string> { "Gluten", "Nuts" },
            DietaryRestrictions = "Vegetarian",
            IsAvailable = false
        };

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Name.Should().Be("Updated Name");
        result.Value.Description.Should().Be("Updated Description");
        result.Value.Price.Should().Be(25.00m);
        result.Value.Category.Should().Be("Dessert");
        result.Value.Allergens.Should().HaveCount(2);
        result.Value.IsAvailable.Should().BeFalse();
        result.Value.UpdatedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task Handle_NonExistentProduct_ReturnsFailure()
    {
        // Arrange
        var context = TestDbContextFactory.CreateInMemoryContext();
        var handler = new UpdateProduct.Handler(context);
        var nonExistentId = Guid.NewGuid();
        
        var command = new UpdateProduct.Command
        {
            Id = nonExistentId,
            Name = "Test",
            Description = "Test",
            Price = 10.00m,
            Category = "Main",
            Allergens = new List<string>(),
            IsAvailable = true
        };

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain(nonExistentId.ToString());
        result.Error.Should().Contain("not found");
    }

    [Fact]
    public async Task Handle_ValidCommand_UpdatesProductInDatabase()
    {
        // Arrange
        var context = TestDbContextFactory.CreateInMemoryContext();
        var productId = Guid.NewGuid();
        
        var product = new Product
        {
            Id = productId,
            Name = "Original",
            Description = "Original",
            Price = 10.00m,
            Category = "Snack",
            Allergens = new List<string>(),
            IsAvailable = true,
            CreatedAt = DateTime.UtcNow
        };
        context.Products.Add(product);
        await context.SaveChangesAsync();
        
        var handler = new UpdateProduct.Handler(context);
        var command = new UpdateProduct.Command
        {
            Id = productId,
            Name = "Modified",
            Description = "Modified",
            Price = 15.00m,
            Category = "Drink",
            Allergens = new List<string> { "Lactose" },
            IsAvailable = false
        };

        // Act
        await handler.Handle(command, CancellationToken.None);

        // Assert
        var updatedProduct = await context.Products.FindAsync(productId);
        updatedProduct.Should().NotBeNull();
        updatedProduct!.Name.Should().Be("Modified");
        updatedProduct.Price.Should().Be(15.00m);
        updatedProduct.Category.Should().Be("Drink");
        updatedProduct.IsAvailable.Should().BeFalse();
    }

    [Fact]
    public async Task Validator_Rejects_Empty_Name()
    {
        // Arrange
        var validator = new UpdateProduct.Validator();
        var command = new UpdateProduct.Command
        {
            Id = Guid.NewGuid(),
            Name = "",
            Description = "Test",
            Price = 10.00m,
            Category = "Main"
        };

        // Act
        var result = validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Name");
    }

    [Fact]
    public async Task Validator_Rejects_Zero_Price()
    {
        // Arrange
        var validator = new UpdateProduct.Validator();
        var command = new UpdateProduct.Command
        {
            Id = Guid.NewGuid(),
            Name = "Test",
            Description = "Test",
            Price = 0,
            Category = "Main"
        };

        // Act
        var result = validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Price");
    }

    [Fact]
    public async Task Validator_Rejects_Invalid_Category()
    {
        // Arrange
        var validator = new UpdateProduct.Validator();
        var command = new UpdateProduct.Command
        {
            Id = Guid.NewGuid(),
            Name = "Test",
            Description = "Test",
            Price = 10.00m,
            Category = "InvalidCategory"
        };

        // Act
        var result = validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Category");
    }

    [Fact]
    public async Task Validator_Rejects_Too_Long_Description()
    {
        // Arrange
        var validator = new UpdateProduct.Validator();
        var command = new UpdateProduct.Command
        {
            Id = Guid.NewGuid(),
            Name = "Test",
            Description = new string('a', 501), // 501 characters
            Price = 10.00m,
            Category = "Main"
        };

        // Act
        var result = validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Description");
    }

    [Fact]
    public async Task Validator_Rejects_Price_Over_1000()
    {
        // Arrange
        var validator = new UpdateProduct.Validator();
        var command = new UpdateProduct.Command
        {
            Id = Guid.NewGuid(),
            Name = "Test",
            Description = "Test",
            Price = 1001,
            Category = "Main"
        };

        // Act
        var result = validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Price");
    }
}