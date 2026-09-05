namespace Application.DTOs
{
    public class ProductToReturnDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public string PictureUrl { get; set; } = string.Empty;

        public string CategoryName { get; set; } = string.Empty;
        public string BrandName { get; set; } = string.Empty;

        // الفرونت إند لازم ياخد القيمة دي ويرجعها تاني وقت أي Update
        public string RowVersion { get; set; } = string.Empty;
    }
}