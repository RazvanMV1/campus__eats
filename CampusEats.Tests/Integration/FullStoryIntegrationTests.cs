using CampusEats.Backend.Common.Services;
using CampusEats.Backend.Domain;
using CampusEats.Backend.Features.Authentication;
using CampusEats.Backend.Features.Kitchen;
using CampusEats.Backend.Features.Menu;
using CampusEats.Backend.Features.Orders;
using CampusEats.Backend.Features.Payments;
using CampusEats.Tests.Helpers;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using Stripe;

namespace CampusEats.Tests.Integration;

public class FullStoryIntegrationTests
{
    [Fact]
    public async Task FullUserJourney_Register_Order_PayCash_Complete_Integration()
    {
        // 1. Setup Infrastructure
        var context = TestDbContextFactory.CreateInMemoryContext();
        
        // Stripe client can be null if we only use Cash
        StripeClient stripeClient = null!; 

        // 2. Register Handler (Only uses AppDbContext)
        var registerHandler = new Register.Handler(context);
        var registerCommand = new Register.Command
        {
            Email = "integration@campus.ro",
            Password = "Password123!",
            FullName = "Integration User",
            PhoneNumber = "+40700000000"
        };
        
        var registerResult = await registerHandler.Handle(registerCommand, CancellationToken.None);
        registerResult.IsSuccess.Should().BeTrue();
        var userId = registerResult.Value!.Id;

        // 3. Admin adds Product
        var createProductHandler = new CreateProduct.Handler(context);
        var createProductCommand = new CreateProduct.Command
        {
            Name = "Integration Burger",
            Description = "Best burger",
            Price = 50m,
            Category = "Main",
            IsAvailable = true
        };
        var productResult = await createProductHandler.Handle(createProductCommand, CancellationToken.None);
        productResult.IsSuccess.Should().BeTrue();
        var productId = productResult.Value!.Id;

        // 4. User Creates Order
        var createOrderHandler = new CreateOrder.Handler(context);
        var createOrderCommand = new CreateOrder.Command
        {
            UserId = userId,
            PaymentMethod = "Cash",
            Items = new List<CreateOrder.OrderItemRequest>
            {
                new() { ProductId = productId, Quantity = 2 } // 2 * 50 = 100
            }
        };

        var orderResult = await createOrderHandler.Handle(createOrderCommand, CancellationToken.None);
        orderResult.IsSuccess.Should().BeTrue();
        var orderId = orderResult.Value!.Id;
        orderResult.Value.TotalAmount.Should().Be(100m);
        orderResult.Value.Status.Should().Be("Pending");

        // 5. User Pays (Cash)
        var processPaymentHandler = new ProcessPayment.Handler(context, stripeClient);
        var paymentCommand = new ProcessPayment.Command
        {
            OrderId = orderId,
            PaymentMethod = "Cash",
            Amount = 100m
        };

        var paymentResult = await processPaymentHandler.Handle(paymentCommand, CancellationToken.None);
        paymentResult.IsSuccess.Should().BeTrue();
        
        // 6. Kitchen Updates Order Status
        var updateStatusHandler = new UpdateOrderStatus.Handler(context);
        
        // Move to Preparing
        await updateStatusHandler.Handle(new UpdateOrderStatus.Command { OrderId = orderId, Status = "Preparing" }, CancellationToken.None);
        
        // Move to Ready
        await updateStatusHandler.Handle(new UpdateOrderStatus.Command { OrderId = orderId, Status = "Ready" }, CancellationToken.None);

        // Move to Completed
        var completeResult = await updateStatusHandler.Handle(new UpdateOrderStatus.Command { OrderId = orderId, Status = "Completed" }, CancellationToken.None);
        completeResult.IsSuccess.Should().BeTrue();
        
        var finalOrder = await context.Orders.FindAsync(orderId);
        finalOrder!.Status.Should().Be("Completed");
    }
}
