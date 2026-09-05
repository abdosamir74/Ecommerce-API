using Application.Common.Interfaces;
using Application.Helpers;
using Domain.Entities.OrderAggregate;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Stripe;

namespace Ecommerce.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class StripeWebhookController : ControllerBase
    {
        private readonly IOrderService _orderService;
        private readonly StripeSettings _stripeSettings;

        public StripeWebhookController(IOrderService orderService, IOptions<StripeSettings> stripeSettings)
        {
            _orderService = orderService;
            _stripeSettings = stripeSettings.Value;
        }

        [HttpPost]
        public async Task<IActionResult> StripeWebhook()
        {
            var json = await new StreamReader(HttpContext.Request.Body).ReadToEndAsync();

            try
            {
                var stripeEvent = EventUtility.ConstructEvent(
                    json,
                    Request.Headers["Stripe-Signature"],
                    _stripeSettings.WhSecret
                );

                PaymentIntent intent;

                switch (stripeEvent.Type)
                {
                    case EventTypes.PaymentIntentSucceeded:
                        intent = (PaymentIntent)stripeEvent.Data.Object;
                        // التعديل هنا: استخدام ميثود الخدمة المقيدة بقواعد الـ Domain
                        await _orderService.UpdateOrderPaymentStatusAsync(intent.Id, OrderStatus.PaymentReceived);
                        break;

                    case EventTypes.PaymentIntentPaymentFailed:
                        intent = (PaymentIntent)stripeEvent.Data.Object;
                        // التعديل هنا: التحديث الآمن عند فشل العملية
                        await _orderService.UpdateOrderPaymentStatusAsync(intent.Id, OrderStatus.PaymentFailed);
                        break;
                }

                return Ok();
            }
            catch (StripeException ex)
            {
                return BadRequest($"Webhook Error: {ex.Message}");
            }
        }
    }
}