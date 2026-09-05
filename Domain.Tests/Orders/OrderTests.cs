using Domain.Entities.OrderAggregate;
using System;
using System.Collections.Generic;
using Xunit;

namespace Domain.Tests.Orders
{
    public class OrderTests
    {
        [Fact]
        public void UpdateStatus_ValidTransition_ShouldUpdateStatusSuccessfully()
        {
            // Arrange
            var address = new Address("FirstName", "LastName", "123 Main St", "Cairo", "Cairo", "12345");
            var items = new List<OrderItem>
            {
                new OrderItem(new ProductItemOrdered(1, "Product 1", "url"), 100, 1)
            };

            // تمرير discount = 0 (أو توضيح البرامترات)
            var order = new Order("test@example.com", address, items, 100, 0, "pi_123");

            // Act
            order.UpdateStatus(OrderStatus.PaymentReceived);

            // Assert
            Assert.Equal(OrderStatus.PaymentReceived, order.Status);
        }

        [Fact]
        public void UpdateStatus_InvalidTransition_ShouldThrowInvalidOperationException()
        {
            // Arrange
            var address = new Address("FirstName", "LastName", "123 Main St", "Cairo", "Cairo", "12345");
            var items = new List<OrderItem>
            {
                new OrderItem(new ProductItemOrdered(1, "Product 1", "url"), 100, 1)
            };

            // تمرير discount = 0
            var order = new Order("test@example.com", address, items, 100, 0, "pi_123");

            // Act & Assert
            var exception = Assert.Throws<InvalidOperationException>(() =>
                order.UpdateStatus(OrderStatus.Delivered)
            );

            Assert.Contains("Cannot transition order status", exception.Message);
        }

        [Fact]
        public void GetTotal_WithDiscount_ShouldCalculateCorrectTotal()
        {
            // Arrange
            var address = new Address("FirstName", "LastName", "123 Main St", "Cairo", "Cairo", "12345");
            var items = new List<OrderItem>
            {
                new OrderItem(new ProductItemOrdered(1, "Product 1", "url"), 100, 1)
            };

            // Act: إنشاء طلب بـ Subtotal = 100 و Discount = 20
            var order = new Order("test@example.com", address, items, 100, 20, "pi_123");

            // Assert
            Assert.Equal(80, order.GetTotal());
        }
    }
}