using System.ComponentModel.DataAnnotations;

namespace Application.DTOs
{
    public class UpdateProductDto
    {
        [Required]
        [MaxLength(200)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(2000)]
        public string Description { get; set; } = string.Empty;

        [Range(0.01, double.MaxValue, ErrorMessage = "السعر لازم يكون أكبر من صفر")]
        public decimal Price { get; set; }

        [Range(0, int.MaxValue)]
        public int Stock { get; set; }

        [Required]
        public int CategoryId { get; set; }

        [Required]
        public int BrandId { get; set; }

        // اليوزر لازم يبعت نفس RowVersion اللي جالها وقت ما جاب المنتج (من GetProduct)
        // عشان نتأكد إن حد تاني مغيرش المنتج من ساعتها
        [Required]
        public string RowVersion { get; set; } = string.Empty;
    }
}