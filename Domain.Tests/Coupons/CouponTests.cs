using Domain.Entities;
using Xunit;

namespace Domain.Tests.Coupons;

public class CouponTests
{
    [Fact]
    public void CalculateDiscount_PercentageDiscount_CalculatesCorrectAmount()
    {
        // Arrange
        var coupon = new Coupon
        {
            DiscountAmount = 20, // 20%
            IsPercentage = true
        };
        decimal subtotal = 150m;

        // Act
        var discount = coupon.CalculateDiscount(subtotal);

        // Assert
        Assert.Equal(30m, discount); // 20% of 150 = 30
    }

    [Fact]
    public void IsValid_ExpiredCoupon_ReturnsFalse()
    {
        // Arrange
        var coupon = new Coupon
        {
            IsActive = true,
            ExpiryDate = DateTime.UtcNow.AddDays(-1)
        };

        // Act
        var isValid = coupon.IsValid();

        // Assert
        Assert.False(isValid);
    }
}