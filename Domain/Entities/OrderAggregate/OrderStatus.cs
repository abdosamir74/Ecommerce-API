using System.Runtime.Serialization;

namespace Domain.Entities.OrderAggregate;

public enum OrderStatus
{
    [EnumMember(Value = "Pending")]
    Pending,

    [EnumMember(Value = "Payment Received")]
    PaymentReceived,

    [EnumMember(Value = "Payment Failed")]
    PaymentFailed,

    [EnumMember(Value = "Processing")]
    Processing,

    [EnumMember(Value = "Shipped")]
    Shipped,

    [EnumMember(Value = "Delivered")]
    Delivered,

    [EnumMember(Value = "Cancelled")]
    Cancelled
}

public static class OrderStateMachine
{
    // قاموس بيحدد الحالات المسموح الانتقال إليها من كل حالة
    private static readonly Dictionary<OrderStatus, List<OrderStatus>> AllowedTransitions = new()
    {
        { OrderStatus.Pending, new List<OrderStatus> { OrderStatus.PaymentReceived, OrderStatus.PaymentFailed, OrderStatus.Cancelled } },
        { OrderStatus.PaymentFailed, new List<OrderStatus> { OrderStatus.Pending, OrderStatus.Cancelled } },
        { OrderStatus.PaymentReceived, new List<OrderStatus> { OrderStatus.Processing, OrderStatus.Cancelled } },
        { OrderStatus.Processing, new List<OrderStatus> { OrderStatus.Shipped, OrderStatus.Cancelled } },
        { OrderStatus.Shipped, new List<OrderStatus> { OrderStatus.Delivered } },
        { OrderStatus.Delivered, new List<OrderStatus>() }, // حالة نهائية
        { OrderStatus.Cancelled, new List<OrderStatus>() }  // حالة نهائية
    };

    public static bool CanTransition(OrderStatus currentStatus, OrderStatus newStatus)
    {
        return AllowedTransitions.TryGetValue(currentStatus, out var allowed) && allowed.Contains(newStatus);
    }
}