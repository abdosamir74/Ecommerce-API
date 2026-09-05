using Application.DTOs.Orders;
using Domain.Entities.OrderAggregate;

namespace Application.Common.Interfaces
{
    public interface IOrderService
    {
        Task<OrderToReturnDto?> CreateOrderAsync(string buyerEmail, string basketId, AddressDto shippingAddress);
        Task<IReadOnlyList<OrderToReturnDto>> GetOrdersForUserAsync(string buyerEmail);
        Task<Order?> GetOrderByIdAsync(int id, string buyerEmail);
        Task<OrderToReturnDto?> GetOrderByIdForUserAsync(int id, string buyerEmail);
        Task<OrderToReturnDto?> UpdateOrderStatusAsync(int orderId, OrderStatus newStatus);
        Task<Order?> UpdateOrderPaymentStatusAsync(string paymentIntentId, OrderStatus status);
    }
}