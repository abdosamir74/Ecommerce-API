using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.DTOs
{
    public class CouponDto
    {
        public int Id { get; set; }
        public string Code { get; set; } = string.Empty;
        public decimal DiscountPercentage { get; set; }
        public DateTime ExpirationDate { get; set; }
        public int MaxUsage { get; set; }
        public int TimesUsed { get; set; }
        public bool IsActive { get; set; }
    }
}
