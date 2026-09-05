using Application.Common.Interfaces;
using Application.Helpers;
using Domain.Entities;
using Ecommerce.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Stripe;

namespace Infrastructure.Services;

public class PaymentService : IPaymentService
{
    private readonly IBasketRepository _basketRepository;
    private readonly ApplicationDbContext _context;
    private readonly StripeSettings _stripeSettings;

    public PaymentService(
        IBasketRepository basketRepository,
        ApplicationDbContext context,
        IOptions<StripeSettings> stripeSettings)
    {
        _basketRepository = basketRepository;
        _context = context;
        _stripeSettings = stripeSettings.Value;
    }

    public async Task<CustomerBasket?> CreateOrUpdatePaymentIntent(string basketId)
    {
        StripeConfiguration.ApiKey = _stripeSettings.SecretKey;

        var basket = await _basketRepository.GetBasketAsync(basketId);
        if (basket == null) return null;

        // تحسين الأداء: جلب كل المنتجات دفعة واحدة
        var productIds = basket.Items.Select(i => i.Id).ToList();
        var dbProducts = await _context.Products
            .Where(p => productIds.Contains(p.Id))
            .ToDictionaryAsync(p => p.Id);

        foreach (var item in basket.Items)
        {
            if (dbProducts.TryGetValue(item.Id, out var productItem))
            {
                if (item.Price != productItem.Price)
                {
                    item.Price = productItem.Price;
                }
            }
        }

        // 1. حساب الإجمالي واقتطاع الخصم
        var subtotal = basket.Items.Sum(i => i.Quantity * (i.Price * 100));
        var discountAmount = (basket.Discount * 100); // تخصيم الخصم إن وجد
        var amount = (long)Math.Max(0, subtotal - discountAmount);

        // 2. معالجة الوضع المحلي (Mocking) لتفادي StripeException عند استخدام مفتاح غير صحيح
        if (string.IsNullOrEmpty(_stripeSettings.SecretKey) || _stripeSettings.SecretKey.Contains("KEY"))
        {
            basket.PaymentIntentId ??= "pi_mock_" + Guid.NewGuid().ToString("N");
            basket.ClientSecret ??= "secret_mock_" + Guid.NewGuid().ToString("N");

            await _basketRepository.UpdateBasketAsync(basket);
            return basket;
        }

        // 3. الاتصال الفعلي بـ Stripe في حالة توفر مفتاح صحيح
        var service = new PaymentIntentService();
        PaymentIntent intent;

        if (string.IsNullOrEmpty(basket.PaymentIntentId))
        {
            var options = new PaymentIntentCreateOptions
            {
                Amount = amount,
                Currency = "usd",
                PaymentMethodTypes = new List<string> { "card" }
            };

            var requestOptions = new RequestOptions
            {
                IdempotencyKey = $"basket-{basket.Id}"
            };

            intent = await service.CreateAsync(options, requestOptions);
            basket.PaymentIntentId = intent.Id;
            basket.ClientSecret = intent.ClientSecret;
        }
        else
        {
            var options = new PaymentIntentUpdateOptions
            {
                Amount = amount
            };
            await service.UpdateAsync(basket.PaymentIntentId, options);
        }

        await _basketRepository.UpdateBasketAsync(basket);
        return basket;
    }
}