using Application.Authorization;
using Application.Common.Interfaces;
using Application.Common.Specifications;
using Application.DTOs;
using Application.Errors;
using Application.Helpers;
using Microsoft.AspNetCore.Hosting;
using AutoMapper;
using Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;

namespace Ecommerce.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProductsController : ControllerBase
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly IWebHostEnvironment _env;

    public ProductsController(IUnitOfWork unitOfWork, IMapper mapper, IWebHostEnvironment env)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _env = env;
    }

    [HttpGet]
    [EnableRateLimiting("fixed")]
    [ResponseCache(Duration = 60, Location = ResponseCacheLocation.Any, VaryByQueryKeys = new[] { "*" })]
    public async Task<ActionResult<Pagination<ProductToReturnDto>>> GetProducts([FromQuery] ProductSpecParams specParams)
    {
        var spec = new ProductsWithTypesAndBrandsSpecification(specParams);
        var countSpec = new ProductWithFiltersForCountSpecification(specParams);

        var totalItems = await _unitOfWork.Repository<Product>().CountAsync(countSpec);
        var products = await _unitOfWork.Repository<Product>().ListAsync(spec);

        var data = _mapper.Map<IReadOnlyList<Product>, IReadOnlyList<ProductToReturnDto>>(products);

        return Ok(new Pagination<ProductToReturnDto>(specParams.PageIndex, specParams.PageSize, totalItems, data));
    }

    [HttpGet("{id}")]
    [EnableRateLimiting("fixed")]
    [ResponseCache(Duration = 60, Location = ResponseCacheLocation.Any)]
    public async Task<ActionResult<ProductToReturnDto>> GetProduct(int id)
    {
        var spec = new ProductsWithTypesAndBrandsSpecification(id);
        var product = await _unitOfWork.Repository<Product>().GetEntityWithSpec(spec);

        if (product == null) return NotFound(new ApiResponse(404, "المنتج غير موجود"));

        return Ok(_mapper.Map<Product, ProductToReturnDto>(product));
    }

    [HttpPost]
    [Authorize(Policy = Permissions.Products.Create)]
    public async Task<ActionResult<ProductToReturnDto>> CreateProduct(CreateProductDto createDto)
    {
        var category = await _unitOfWork.Repository<Category>().GetByIdAsync(createDto.CategoryId);
        if (category == null) return BadRequest(new ApiResponse(400, "الـ Category المحددة غير موجودة"));

        var brand = await _unitOfWork.Repository<Brand>().GetByIdAsync(createDto.BrandId);
        if (brand == null) return BadRequest(new ApiResponse(400, "الـ Brand المحدد غير موجود"));

        var product = _mapper.Map<CreateProductDto, Product>(createDto);

        await _unitOfWork.Repository<Product>().AddAsync(product);
        var result = await _unitOfWork.CompleteAsync();

        if (result <= 0) return BadRequest(new ApiResponse(400, "فشل إنشاء المنتج"));

        var spec = new ProductsWithTypesAndBrandsSpecification(product.Id);
        var createdProduct = await _unitOfWork.Repository<Product>().GetEntityWithSpec(spec);

        return CreatedAtAction(nameof(GetProduct), new { id = product.Id },
            _mapper.Map<Product, ProductToReturnDto>(createdProduct));
    }

    [HttpPut("{id:int}")]
    [Authorize(Policy = Permissions.Products.Update)]
    public async Task<ActionResult<ProductToReturnDto>> UpdateProduct(int id, UpdateProductDto updateDto)
    {
        var product = await _unitOfWork.Repository<Product>().GetByIdAsync(id);
        if (product == null) return NotFound(new ApiResponse(404, "المنتج غير موجود"));

        var category = await _unitOfWork.Repository<Category>().GetByIdAsync(updateDto.CategoryId);
        if (category == null) return BadRequest(new ApiResponse(400, "الـ Category المحددة غير موجودة"));

        var brand = await _unitOfWork.Repository<Brand>().GetByIdAsync(updateDto.BrandId);
        if (brand == null) return BadRequest(new ApiResponse(400, "الـ Brand المحدد غير موجود"));

        var clientRowVersion = Convert.FromBase64String(updateDto.RowVersion);
        _unitOfWork.SetOriginalRowVersion(product, clientRowVersion);

        product.Name = updateDto.Name;
        product.Description = updateDto.Description;
        product.Price = updateDto.Price;
        product.Stock = updateDto.Stock;
        product.CategoryId = updateDto.CategoryId;
        product.BrandId = updateDto.BrandId;

        try
        {
            await _unitOfWork.CompleteAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            return Conflict(new ApiResponse(409,
                "المنتج اتعدل من حد تاني قبلك، افتح البيانات تاني وحاول تعدل من جديد"));
        }

        var spec = new ProductsWithTypesAndBrandsSpecification(product.Id);
        var updatedProduct = await _unitOfWork.Repository<Product>().GetEntityWithSpec(spec);

        return Ok(_mapper.Map<Product, ProductToReturnDto>(updatedProduct));
    }

    [HttpDelete("{id:int}")]
    [Authorize(Policy = Permissions.Products.Delete)]
    public async Task<ActionResult> DeleteProduct(int id)
    {
        var product = await _unitOfWork.Repository<Product>().GetByIdAsync(id);
        if (product == null) return NotFound(new ApiResponse(404, "المنتج غير موجود"));

        _unitOfWork.Repository<Product>().Delete(product);
        var result = await _unitOfWork.CompleteAsync();

        if (result <= 0) return BadRequest(new ApiResponse(400, "فشل حذف المنتج"));

        return NoContent();
    }

    [HttpPost("{id:int}/image")]
    [Authorize(Policy = Permissions.Products.Update)]
    public async Task<ActionResult<ProductToReturnDto>> UploadProductImage(int id, IFormFile file)
    {
        var product = await _unitOfWork.Repository<Product>().GetByIdAsync(id);
        if (product == null) return NotFound(new ApiResponse(404, "المنتج غير موجود"));

        if (file == null || file.Length == 0)
            return BadRequest(new ApiResponse(400, "لازم ترفع ملف صورة"));

        var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".webp" };
        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!allowedExtensions.Contains(extension))
            return BadRequest(new ApiResponse(400, "امتداد الملف غير مسموح به. المسموح: jpg, jpeg, png, webp"));

        const long maxFileSize = 2 * 1024 * 1024;
        if (file.Length > maxFileSize)
            return BadRequest(new ApiResponse(400, "حجم الصورة أكبر من المسموح (2 ميجا)"));

        var fileName = $"{Guid.NewGuid()}{extension}";
        var folderPath = Path.Combine(_env.WebRootPath, "images", "products");
        Directory.CreateDirectory(folderPath);

        var filePath = Path.Combine(folderPath, fileName);

        using (var stream = new FileStream(filePath, FileMode.Create))
        {
            await file.CopyToAsync(stream);
        }

        var oldPictureUrl = product.PictureUrl;
        product.PictureUrl = $"/images/products/{fileName}";

        try
        {
            await _unitOfWork.CompleteAsync();
        }
        catch
        {
            if (System.IO.File.Exists(filePath)) System.IO.File.Delete(filePath);
            throw;
        }

        if (!string.IsNullOrWhiteSpace(oldPictureUrl) && oldPictureUrl.StartsWith("/images/products/", StringComparison.OrdinalIgnoreCase))
        {
            var oldPath = Path.Combine(_env.WebRootPath, oldPictureUrl.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
            if (System.IO.File.Exists(oldPath)) System.IO.File.Delete(oldPath);
        }

        var spec = new ProductsWithTypesAndBrandsSpecification(product.Id);
        var updatedProduct = await _unitOfWork.Repository<Product>().GetEntityWithSpec(spec);

        return Ok(_mapper.Map<Product, ProductToReturnDto>(updatedProduct));
    }
}