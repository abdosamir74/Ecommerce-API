using System;
using System.Collections.Generic;

namespace Domain.Entities
{
    public class CustomerBasket
    {
        public string Id { get; set; } = string.Empty;
        public List<BasketItem> Items { get; set; } = new List<BasketItem>();

        // خواص الدفع (Stripe)
        public string? PaymentIntentId { get; set; }
        public string? ClientSecret { get; set; }

        // خواص الكوبون والخصم
        public string? CouponCode { get; set; }
        public decimal Discount { get; set; }

        public CustomerBasket() { }

        public CustomerBasket(string id)
        {
            Id = id;
        }
    }
}