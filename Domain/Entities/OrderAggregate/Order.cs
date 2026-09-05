using Domain.Common;

namespace Domain.Entities.OrderAggregate
{
    public class Order : BaseEntity
    {
        // Parameterless constructor for EF Core
        private Order() { }

        public Order(
            string buyerEmail,
            Address shipToAddress,
            IReadOnlyList<OrderItem> orderItems,
            decimal subtotal,
            decimal discount = 0,
            string? paymentIntentId = null)
        {
            BuyerEmail = buyerEmail;
            ShipToAddress = shipToAddress;
            OrderItems = orderItems ?? new List<OrderItem>();
            Subtotal = subtotal;
            Discount = discount;
            PaymentIntentId = paymentIntentId;
            Status = OrderStatus.Pending;
            OrderDate = DateTimeOffset.UtcNow;
        }

        public string BuyerEmail { get; private set; } = string.Empty;
        public DateTimeOffset OrderDate { get; private set; } = DateTimeOffset.UtcNow;
        public Address ShipToAddress { get; private set; } = null!;
        public OrderStatus Status { get; private set; } = OrderStatus.Pending;
        public IReadOnlyList<OrderItem> OrderItems { get; private set; } = new List<OrderItem>();
        public decimal Subtotal { get; private set; }
        public decimal Discount { get; private set; }
        public string? PaymentIntentId { get; private set; }

        public decimal GetTotal() => Subtotal - Discount;

        public void UpdateStatus(OrderStatus newStatus)
        {
            if (!OrderStateMachine.CanTransition(Status, newStatus))
            {
                throw new InvalidOperationException($"Cannot transition order status from {Status} to {newStatus}.");
            }

            Status = newStatus;
        }
    }
}