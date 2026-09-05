using Application.Common.Interfaces;
using Application.DTOs;
using AutoMapper;
using Domain.Entities;
using Ecommerce.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Services
{
    public class CouponService : ICouponService
    {
        private readonly ApplicationDbContext _context;
        private readonly IBasketRepository _basketRepository;
        private readonly IMapper _mapper;

        public CouponService(
            ApplicationDbContext context,
            IBasketRepository basketRepository,
            IMapper mapper)
        {
            _context = context;
            _basketRepository = basketRepository;
            _mapper = mapper;
        }

        public async Task<CouponDto?> CreateCouponAsync(CreateCouponDto createCouponDto)
        {
            var codeUpper = createCouponDto.Code.Trim().ToUpper();

            var existingCoupon = await _context.Set<Coupon>()
                .AnyAsync(c => c.Code == codeUpper);

            if (existingCoupon)
                throw new InvalidOperationException("كوبون الخصم هذا موجود بالفعل.");

            var coupon = new Coupon
            {
                Code = codeUpper,
                DiscountAmount = createCouponDto.DiscountPercentage,
                IsPercentage = true,
                ExpiryDate = createCouponDto.ExpirationDate,
                UsageLimit = createCouponDto.MaxUsage,
                IsActive = true
            };

            _context.Set<Coupon>().Add(coupon);
            await _context.SaveChangesAsync();

            return _mapper.Map<CouponDto>(coupon);
        }

        public async Task<CouponDto?> GetCouponByCodeAsync(string code)
        {
            var coupon = await _context.Set<Coupon>()
                .FirstOrDefaultAsync(c => c.Code == code.ToUpper());

            if (coupon == null) return null;

            return _mapper.Map<CouponDto>(coupon);
        }

        public async Task<IReadOnlyList<CouponDto>> GetAllCouponsAsync()
        {
            var coupons = await _context.Set<Coupon>()
                .ToListAsync();

            return _mapper.Map<IReadOnlyList<CouponDto>>(coupons);
        }

        public async Task<bool> DeleteCouponAsync(int id)
        {
            var coupon = await _context.Set<Coupon>().FindAsync(id);
            if (coupon == null) return false;

            _context.Set<Coupon>().Remove(coupon);
            return await _context.SaveChangesAsync() > 0;
        }

        // --- الدالة المطلوبة لربط الكوبون بالسلة في Redis ---
        public async Task<CustomerBasketDto?> ApplyCouponToBasketAsync(string basketId, string couponCode)
        {
            var basket = await _basketRepository.GetBasketAsync(basketId);
            if (basket == null)
                throw new KeyNotFoundException("السلة غير موجودة.");

            var coupon = await _context.Set<Coupon>()
                .FirstOrDefaultAsync(c => c.Code == couponCode.Trim().ToUpper());

            if (coupon == null || !coupon.IsValid())
            {
                throw new InvalidOperationException("الكوبون غير صالح أو انتهت صلاحيته.");
            }

            // 1. حساب إجمالي السلة
            var subtotal = basket.Items.Sum(i => i.Price * i.Quantity);

            // 2. حساب قيمة الخصم باستخدام ميثود Domain المتاحة في Coupon Entity
            var discountAmount = coupon.CalculateDiscount(subtotal);

            // 3. تحديث بيانات السلة
            basket.CouponCode = coupon.Code;
            basket.Discount = discountAmount;

            var updatedBasket = await _basketRepository.UpdateBasketAsync(basket);
            return _mapper.Map<CustomerBasketDto>(updatedBasket);
        }
    }
}