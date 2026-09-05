using Application.Common.Interfaces;
using Application.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Ecommerce.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CouponsController : ControllerBase
    {
        private readonly ICouponService _couponService;

        public CouponsController(ICouponService couponService)
        {
            _couponService = couponService;
        }

        // إنشاء كوبون جديد (خاص بالأدمن عبر الـ Permission Policy)
        [HttpPost]
        [Authorize(Policy = "Permissions.Coupons.Create")]
        public async Task<ActionResult<CouponDto>> CreateCoupon([FromBody] CreateCouponDto createCouponDto)
        {
            try
            {
                var coupon = await _couponService.CreateCouponAsync(createCouponDto);
                return Ok(coupon);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        // تطبيق الكوبون (متاح لكل العُملاء المسجلين، مع استخراج السلة من الـ Token حمايةً من الـ IDOR)
        [HttpPost("apply")]
        [Authorize]
        public async Task<ActionResult> ApplyCoupon([FromBody] ApplyCouponDto dto)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(userId))
                return Unauthorized(new { message = "غير مصرح، يرجى تسجيل الدخول أولاً." });

            try
            {
                var basket = await _couponService.ApplyCouponToBasketAsync(userId, dto.CouponCode);
                return Ok(basket);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        // عرض جميع الكوبونات (لإدارة الأدمن)
        [HttpGet]
        [Authorize(Policy = "Permissions.Coupons.Read")]
        public async Task<ActionResult<IReadOnlyList<CouponDto>>> GetCoupons()
        {
            var coupons = await _couponService.GetAllCouponsAsync();
            return Ok(coupons);
        }

        // حذف كوبون
        [HttpDelete("{id}")]
        [Authorize(Policy = "Permissions.Coupons.Delete")]
        public async Task<ActionResult> DeleteCoupon(int id)
        {
            var result = await _couponService.DeleteCouponAsync(id);
            if (!result) return NotFound(new { message = "الكوبون غير موجود." });

            return Ok(new { message = "تم حذف الكوبون بنجاح." });
        }
    }
}