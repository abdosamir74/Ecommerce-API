using Domain.Entities;
using Ecommerce.Application.Common.Specifications;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Common.Specifications
{
    public class ProductsWithTypesAndBrandsSpecification : BaseSpecification<Product>
    {
        public ProductsWithTypesAndBrandsSpecification(ProductSpecParams specParams)
            : base(x =>
                (string.IsNullOrEmpty(specParams.Search) || x.Name.ToLower().Contains(specParams.Search)) &&
                (!specParams.BrandId.HasValue || x.BrandId == specParams.BrandId) &&
                (!specParams.CategoryId.HasValue || x.CategoryId == specParams.CategoryId)
            )
        {
            AddInclude(p => p.Brand);
            AddInclude(p => p.Category);

            // تفعيل Query Splitting لمنع Cartesian Explosion
            ApplySplitQuery();

            // الترتيب الافتراضي بالاسم
            AddOrderBy(p => p.Name);

            // تطبيق الصفحات Pagination
            ApplyPaging(specParams.PageSize * (specParams.PageIndex - 1), specParams.PageSize);

            // منطق الترتيب حسب مدخلات المستخدم
            if (!string.IsNullOrEmpty(specParams.Sort))
            {
                switch (specParams.Sort)
                {
                    case "priceAsc":
                        AddOrderBy(p => p.Price);
                        break;
                    case "priceDesc":
                        AddOrderByDescending(p => p.Price);
                        break;
                    default:
                        AddOrderBy(p => p.Name);
                        break;
                }
            }
        }

        public ProductsWithTypesAndBrandsSpecification(int id)
            : base(p => p.Id == id)
        {
            AddInclude(p => p.Brand);
            AddInclude(p => p.Category);

            ApplySplitQuery();
        }
    }
}