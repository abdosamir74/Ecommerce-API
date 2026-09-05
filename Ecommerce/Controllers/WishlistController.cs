using Application.Common.Interfaces;
using Application.DTOs.Wishlist;
using AutoMapper;
using Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Ecommerce.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class WishlistController : ControllerBase
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public WishlistController(IUnitOfWork unitOfWork, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<WishlistProductDto>>> GetUserWishlist()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        var wishlistItems = await _unitOfWork.Repository<WishlistItem>()
            .GetAsync(w => w.AppUserId == userId);

        var productIds = wishlistItems.Select(w => w.ProductId).ToList();

        var products = await _unitOfWork.Repository<Product>()
            .GetAsync(p => productIds.Contains(p.Id));

        return Ok(_mapper.Map<IReadOnlyList<Product>, IReadOnlyList<WishlistProductDto>>(products));
    }

    [HttpPost("{productId}")]
    public async Task<IActionResult> AddToWishlist(int productId)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        var product = await _unitOfWork.Repository<Product>().GetByIdAsync(productId);
        if (product == null) return NotFound("Product not found");

        var existingItem = (await _unitOfWork.Repository<WishlistItem>().GetAllAsync())
            .FirstOrDefault(w => w.AppUserId == userId && w.ProductId == productId);

        if (existingItem != null) return BadRequest("Product is already in wishlist");

        var wishlistItem = new WishlistItem
        {
            AppUserId = userId!,
            ProductId = productId
        };

        await _unitOfWork.Repository<WishlistItem>().AddAsync(wishlistItem);
        var result = await _unitOfWork.CompleteAsync();

        if (result <= 0) return BadRequest("Problem adding product to wishlist");

        return Ok(new { message = "Product added to wishlist successfully" });
    }

    [HttpDelete("{productId}")]
    public async Task<IActionResult> RemoveFromWishlist(int productId)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        var wishlistItem = (await _unitOfWork.Repository<WishlistItem>().GetAllAsync())
            .FirstOrDefault(w => w.AppUserId == userId && w.ProductId == productId);

        if (wishlistItem == null) return NotFound("Product is not in wishlist");

        _unitOfWork.Repository<WishlistItem>().Delete(wishlistItem);
        var result = await _unitOfWork.CompleteAsync();

        if (result <= 0) return BadRequest("Problem removing product from wishlist");

        return Ok(new { message = "Product removed from wishlist successfully" });
    }
}