using Application.Common.Interfaces;
using Application.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

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

        //  إنشاء كوبون جديد (خاص بالأدمن)
        [HttpPost]
        [Authorize(Roles = "Admin")]
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

        [HttpPost("apply/{basketId}")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult> ApplyCoupon(string basketId, [FromBody] string couponCode)
        {
            try
            {
                var basket = await _couponService.ApplyCouponToBasketAsync(basketId, couponCode);
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

        //  عرض جميع الكوبونات (لإدارة الأدمن)
        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<IReadOnlyList<CouponDto>>> GetCoupons()
        {
            var coupons = await _couponService.GetAllCouponsAsync();
            return Ok(coupons);
        }

        //  حذف كوبون
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult> DeleteCoupon(int id)
        {
            var result = await _couponService.DeleteCouponAsync(id);
            if (!result) return NotFound(new { message = "الكوبون غير موجود." });

            return Ok(new { message = "تم حذف الكوبون بنجاح." });
        }
    }
}