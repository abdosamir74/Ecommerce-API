using Domain.Common;
using System;

namespace Domain.Entities
{
    public class Coupon : BaseEntity
    {
        // Parameterless constructor for EF Core
        public Coupon() { }

        // Constructor للإنشاء
        public Coupon(string code, decimal discountAmount, bool isPercentage, DateTime expiryDate, int usageLimit)
        {
            Code = code.Trim().ToUpper();
            DiscountAmount = discountAmount;
            IsPercentage = isPercentage;
            ExpiryDate = expiryDate;
            UsageLimit = usageLimit;
            IsActive = true;
            TimesUsed = 0;
        }

        public string Code { get; set; } = string.Empty;
        public decimal DiscountAmount { get; set; }
        public bool IsPercentage { get; set; }
        public DateTime ExpiryDate { get; set; }
        public bool IsActive { get; set; } = true;
        public int UsageLimit { get; set; }
        public int TimesUsed { get; set; }


        // RowVersion لمنع الـ Race Condition والـ Concurrent Writes
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();

        public bool IsValid()
        {
            return IsActive
                && DateTime.UtcNow <= ExpiryDate
                && TimesUsed < UsageLimit;
        }

        public decimal CalculateDiscount(decimal orderSubtotal)
        {
            if (!IsValid())
                return 0;

            if (IsPercentage)
            {
                return (orderSubtotal * DiscountAmount) / 100;
            }

            return Math.Min(DiscountAmount, orderSubtotal);
        }

        public void IncrementUsage()
        {
            if (!IsValid())
                throw new InvalidOperationException("الكوبون غير صالح للاستخدام.");

            TimesUsed++;
        }
    }
}