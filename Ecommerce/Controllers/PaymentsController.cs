using Application.Common.Interfaces;
using Application.Errors;
using Application.Helpers;
using Domain.Entities;
using Domain.Entities.OrderAggregate;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Stripe;

namespace Ecommerce.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PaymentsController : ControllerBase
{
    private readonly IPaymentService _paymentService;
    private readonly IBasketRepository _basketRepository;
    private readonly IOrderService _orderService;
    private readonly ILogger<PaymentsController> _logger;
    private readonly StripeSettings _stripeSettings;

    public PaymentsController(
        IPaymentService paymentService,
        IBasketRepository basketRepository,
        IOrderService orderService,
        ILogger<PaymentsController> logger,
        IOptions<StripeSettings> stripeSettings)
    {
        _paymentService = paymentService;
        _basketRepository = basketRepository;
        _orderService = orderService;
        _logger = logger;
        _stripeSettings = stripeSettings.Value;
    }

    [Authorize]
    [HttpPost("{basketId}")]
    public async Task<ActionResult<CustomerBasket>> CreateOrUpdatePaymentIntent(string basketId)
    {
        if (string.IsNullOrWhiteSpace(basketId))
            return BadRequest(new ApiResponse(400, "Basket ID is required"));

        var basket = await _paymentService.CreateOrUpdatePaymentIntent(basketId);
        if (basket == null)
            return BadRequest(new ApiResponse(400, "Problem with your basket"));

        return Ok(basket);
    }

    [AllowAnonymous]
    [HttpPost("webhook")]
    public async Task<IActionResult> StripeWebhook()
    {
        var json = await new StreamReader(HttpContext.Request.Body).ReadToEndAsync();

        var signatureHeader = Request.Headers["Stripe-Signature"].ToString();

        // حماية الـ Endpoint من الـ NullReferenceException عند عدم وجود الـ Header أو الـ Secret
        if (string.IsNullOrEmpty(signatureHeader) || string.IsNullOrEmpty(_stripeSettings?.WhSecret))
        {
            _logger.LogWarning("Stripe Webhook attempt failed due to missing Stripe-Signature or WhSecret.");
            return BadRequest(new ApiResponse(400, "Missing Stripe Signature or WhSecret configuration."));
        }

        try
        {
            var stripeEvent = EventUtility.ConstructEvent(
                json,
                signatureHeader,
                _stripeSettings.WhSecret
            );

            PaymentIntent intent;
            Order? order;

            switch (stripeEvent.Type)
            {
                case EventTypes.PaymentIntentSucceeded:
                    intent = (PaymentIntent)stripeEvent.Data.Object;
                    _logger.LogInformation("Payment Succeeded: {Id}", intent.Id);

                    order = await _orderService.UpdateOrderPaymentStatusAsync(intent.Id, OrderStatus.PaymentReceived);
                    _logger.LogInformation("Order status updated to PaymentReceived: {OrderId}", order?.Id);
                    break;

                case EventTypes.PaymentIntentPaymentFailed:
                    intent = (PaymentIntent)stripeEvent.Data.Object;
                    _logger.LogWarning("Payment Failed: {Id}", intent.Id);

                    order = await _orderService.UpdateOrderPaymentStatusAsync(intent.Id, OrderStatus.PaymentFailed);
                    _logger.LogInformation("Order status updated to PaymentFailed: {OrderId}", order?.Id);
                    break;
            }

            return Ok();
        }
        catch (StripeException ex)
        {
            _logger.LogError(ex, "Stripe Webhook Signature Verification Failed");
            return BadRequest(new ApiResponse(400, "Invalid Stripe Signature"));
        }
    }
}