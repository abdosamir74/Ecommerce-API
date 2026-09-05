using Application.Common.Interfaces;
using Application.DTOs;
using AutoMapper;
using Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Ecommerce.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class BasketController : ControllerBase
{
    private readonly IBasketRepository _basketRepository;
    private readonly IMapper _mapper;

    public BasketController(IBasketRepository basketRepository, IMapper mapper)
    {
        _basketRepository = basketRepository;
        _mapper = mapper;
    }

    private string? CurrentUserId() => User.FindFirstValue(ClaimTypes.NameIdentifier);

    [HttpGet]
    public async Task<ActionResult<CustomerBasket>> GetBasketById()
    {
        var userId = CurrentUserId();
        if (string.IsNullOrWhiteSpace(userId)) return Unauthorized();

        var basket = await _basketRepository.GetBasketAsync(userId);
        return Ok(basket ?? new CustomerBasket(userId));
    }

    [HttpPost]
    public async Task<ActionResult<CustomerBasket>> UpdateBasket([FromBody] CustomerBasketDto basketDto)
    {
        var userId = CurrentUserId();
        if (string.IsNullOrWhiteSpace(userId)) return Unauthorized();

        // الحصول على السلة الحالية أو إنشاء سلة جديدة مرتبطة بالـ userId الخاص بالتوكن
        var existingBasket = await _basketRepository.GetBasketAsync(userId) ?? new CustomerBasket(userId);

        // تحويل الـ DTO المضمون وتنفيذ منطق إضافة/تحديث العناصر عبر الـ Domain Model
        var mappedItems = _mapper.Map<List<BasketItem>>(basketDto.Items);

        // إعادة ضبط عناصر السلة باستخدام منطق الـ Domain
        existingBasket.Items = mappedItems;

        var updatedBasket = await _basketRepository.UpdateBasketAsync(existingBasket);
        return Ok(updatedBasket);
    }

    [HttpDelete]
    public async Task<IActionResult> DeleteBasketAsync()
    {
        var userId = CurrentUserId();
        if (string.IsNullOrWhiteSpace(userId)) return Unauthorized();

        await _basketRepository.DeleteBasketAsync(userId);
        return Ok();
    }
}