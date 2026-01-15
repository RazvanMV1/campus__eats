using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CampusEats.Backend.Domain;
using CampusEats.Backend.Features.Loyalty;
using CampusEats.Backend.Persistence;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace CampusEats.Tests.Features.Loyalty
{
    public class AwardLoyaltyPointsTests
    {
        private AppDbContext CreateContext(string dbName)
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(dbName)
                .Options;
            return new AppDbContext(options);
        }

        [Fact]
        public async Task Handler_Fails_When_User_Not_Found()
        {
            // Arrange
            var dbName = Guid.NewGuid().ToString();
            await using var context = CreateContext(dbName);

            var handler = new AwardLoyaltyPoints.Handler(context);
            var command = new AwardLoyaltyPoints.Command(Guid.NewGuid(), Points: 100, Description: "Bonus");

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Contains("not found", result.Errors[0], StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task Handler_Adds_Points_And_Transaction_When_User_Exists()
        {
            // Arrange
            var dbName = Guid.NewGuid().ToString();
            await using var context = CreateContext(dbName);

            var user = new CampusEats.Backend.Domain.User
            {
                Id = Guid.NewGuid(),
                Email = "award@test",
                LoyaltyPoints = 0,
                CreatedAt = DateTime.UtcNow
            };
            context.Users.Add(user);
            await context.SaveChangesAsync();

            var handler = new AwardLoyaltyPoints.Handler(context);
            var command = new AwardLoyaltyPoints.Command(user.Id, Points: 250, Description: "Promo Bonus", OrderId: null);

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            Assert.True(result.IsSuccess);
            var updatedUser = await context.Users.FindAsync(user.Id);
            Assert.Equal(250, updatedUser.LoyaltyPoints);

            var tx = context.LoyaltyTransactions.FirstOrDefault(t => t.UserId == user.Id && t.PointsChange > 0);
            Assert.NotNull(tx);
            Assert.Equal(250, tx.PointsChange);
            Assert.Equal(LoyaltyTransactionType.Earned, tx.Type);
            Assert.Contains("Promo Bonus", tx.Description);
        }

        [Fact]
        public async Task Validator_Rejects_Zero_Points()
        {
            // Arrange
            var validator = new AwardLoyaltyPoints.Validator();
            var command = new AwardLoyaltyPoints.Command(Guid.NewGuid(), Points: 0, Description: "Test");

            // Act
            var result = validator.Validate(command);

            // Assert
            Assert.False(result.IsValid);
            Assert.Contains(result.Errors, e => e.PropertyName == "Points");
        }

        [Fact]
        public async Task Validator_Rejects_Negative_Points()
        {
            // Arrange
            var validator = new AwardLoyaltyPoints.Validator();
            var command = new AwardLoyaltyPoints.Command(Guid.NewGuid(), Points: -50, Description: "Test");

            // Act
            var result = validator.Validate(command);

            // Assert
            Assert.False(result.IsValid);
            Assert.Contains(result.Errors, e => e.PropertyName == "Points");
        }

        [Fact]
        public async Task Validator_Rejects_Empty_Description()
        {
            // Arrange
            var validator = new AwardLoyaltyPoints.Validator();
            var command = new AwardLoyaltyPoints.Command(Guid.NewGuid(), Points: 100, Description: "");

            // Act
            var result = validator.Validate(command);

            // Assert
            Assert.False(result.IsValid);
            Assert.Contains(result.Errors, e => e.PropertyName == "Description");
        }

        [Fact]
        public async Task Handler_Accumulates_Points_Correctly()
        {
            // Arrange
            var dbName = Guid.NewGuid().ToString();
            await using var context = CreateContext(dbName);

            var user = new CampusEats.Backend.Domain.User
            {
                Id = Guid.NewGuid(),
                Email = "accumulate@test",
                LoyaltyPoints = 100, // Starting points
                CreatedAt = DateTime.UtcNow
            };
            context.Users.Add(user);
            await context.SaveChangesAsync();

            var handler = new AwardLoyaltyPoints.Handler(context);
            var command = new AwardLoyaltyPoints.Command(user.Id, Points: 50, Description: "Additional Bonus");

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            Assert.True(result.IsSuccess);
            var updatedUser = await context.Users.FindAsync(user.Id);
            Assert.Equal(150, updatedUser.LoyaltyPoints); // 100 + 50
        }
    }
}