using Application.Common.Interfaces;
using Application.DTOs.Orders;
using Application.Errors;
using Domain.Entities.OrderAggregate;
using Hangfire;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Ecommerce.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class OrdersController : ControllerBase
    {
        private readonly IOrderService _orderService;
        private readonly IBackgroundJobClient _backgroundJobClient;

        public OrdersController(IOrderService orderService, IBackgroundJobClient backgroundJobClient)
        {
            _orderService = orderService;
            _backgroundJobClient = backgroundJobClient;
        }

        [HttpPost]
        public async Task<ActionResult<OrderToReturnDto>> CreateOrder(OrderDto orderDto)
        {
            var email = User.FindFirstValue(ClaimTypes.Email);

            var order = await _orderService.CreateOrderAsync(email!, orderDto.BasketId, orderDto.ShipToAddress);

            if (order == null) return BadRequest(new ApiResponse(400, "Problem creating order"));

            // إرسال إيميل تأكيد الطلب عبر Hangfire Background Job
            _backgroundJobClient.Enqueue<IEmailService>(emailService =>
                emailService.SendEmailAsync(
                    email!,
                    "Order Confirmation",
                    $"<h1>Thank you for your order!</h1><p>Your order ID is <strong>{order.Id}</strong> and total amount is <strong>${order.Total}</strong>.</p>"
                ));

            return Ok(order);
        }

        [HttpGet]
        public async Task<ActionResult<IReadOnlyList<OrderToReturnDto>>> GetOrdersForUser()
        {
            var email = User.FindFirstValue(ClaimTypes.Email);

            var orders = await _orderService.GetOrdersForUserAsync(email!);

            return Ok(orders);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<OrderToReturnDto>> GetOrderByIdForUser(int id)
        {
            var email = User.FindFirstValue(ClaimTypes.Email);

            var order = await _orderService.GetOrderByIdForUserAsync(id, email!);

            if (order == null) return NotFound(new ApiResponse(404, "Order not found"));

            return Ok(order);
        }

        [HttpPut("{id}/status")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<OrderToReturnDto>> UpdateOrderStatus(int id, [FromBody] OrderStatus newStatus)
        {
            var updatedOrder = await _orderService.UpdateOrderStatusAsync(id, newStatus);

            if (updatedOrder == null)
                return NotFound(new ApiResponse(404, "Order not found."));

            return Ok(updatedOrder);
        }
    }
}