using Application.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Common.Interfaces
{
    public interface ICouponService
    {
        Task<CouponDto?> CreateCouponAsync(CreateCouponDto createCouponDto);
        Task<CouponDto?> GetCouponByCodeAsync(string code);
        Task<IReadOnlyList<CouponDto>> GetAllCouponsAsync();
        Task<bool> DeleteCouponAsync(int id);
        Task<CustomerBasketDto?> ApplyCouponToBasketAsync(string basketId, string couponCode);
    }
}
