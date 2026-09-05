using Domain.Entities.OrderAggregate;
using Xunit;

namespace Domain.Tests.Orders;

public class OrderStateMachineTests
{
    [Theory]
    // 1. الانتقالات المسموحة (Valid Transitions)
    [InlineData(OrderStatus.Pending, OrderStatus.PaymentReceived, true)]
    [InlineData(OrderStatus.Pending, OrderStatus.PaymentFailed, true)]
    [InlineData(OrderStatus.Pending, OrderStatus.Cancelled, true)]
    [InlineData(OrderStatus.PaymentFailed, OrderStatus.Pending, true)]
    [InlineData(OrderStatus.PaymentReceived, OrderStatus.Processing, true)]
    [InlineData(OrderStatus.Processing, OrderStatus.Shipped, true)]
    [InlineData(OrderStatus.Shipped, OrderStatus.Delivered, true)]

    // 2. الانتقالات الغير مسموحة (Invalid Transitions)
    [InlineData(OrderStatus.Pending, OrderStatus.Shipped, false)]
    [InlineData(OrderStatus.Pending, OrderStatus.Delivered, false)]
    [InlineData(OrderStatus.PaymentReceived, OrderStatus.Delivered, false)]
    [InlineData(OrderStatus.Delivered, OrderStatus.Cancelled, false)]
    [InlineData(OrderStatus.Cancelled, OrderStatus.Pending, false)]
    public void CanTransition_ShouldValidateStatusRules(OrderStatus current, OrderStatus next, bool expected)
    {
        // Act
        var result = OrderStateMachine.CanTransition(current, next);

        // Assert
        Assert.Equal(expected, result);
    }
}