using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.DTOs
{
    public class CreateCouponDto
    {
        [Required]
        [StringLength(20, MinimumLength = 3)]
        public string Code { get; set; } = string.Empty;

        [Required]
        [Range(0.01, 100.00, ErrorMessage = "نسبة الخصم يجب أن تكون بين 0.01% و 100%")]
        public decimal DiscountPercentage { get; set; }

        [Required]
        public DateTime ExpirationDate { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "الحد الأقصى للاستخدام يجب أن يكون 1 على الأقل")]
        public int MaxUsage { get; set; } = 100;
    }
}
