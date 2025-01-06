using System.Security.Claims;
using AutoMapper;
using Clothes.Data;
using Clothes.Models.DTOs.CartDTOs;
using Clothes.Models.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Clothes.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class CartsController : ControllerBase
{
    private readonly ApplicationDbContext dbContext;
    private readonly IMapper mapper;
    private readonly RedisCacheService cacheService;
    
    public CartsController(ApplicationDbContext dbContext, IMapper mapper, RedisCacheService cacheService)
    {
        this.dbContext = dbContext;
        this.mapper = mapper;
        this.cacheService = cacheService;
    }
    
    [HttpGet]
    public async Task<IActionResult> GetCart()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (userId is null)
        {
            return Unauthorized("User is not authenticated.");
        }
        
        var cacheKey = $"Cart_{userId}";
        var cachedCart = await cacheService.GetRecordAsync<CartDTO>(cacheKey);
        
        if (cachedCart != null)
        {
            return Ok(cachedCart);
        }

        var cart = await dbContext.Carts
            .Include(c => c.Items)
            .ThenInclude(ci => ci.Clothes)
            .FirstOrDefaultAsync(c => c.UserId == userId);

        if (cart == null)
        {
            return NotFound("Cart not found");
        }

        var cartDto = mapper.Map<CartDTO>(cart);
        
        await cacheService.SetRecordAsync(cacheKey, cartDto);

        return Ok(cartDto);
    }
    
    [HttpPost("add")]
    public async Task<IActionResult> AddToCart([FromBody] AddToCartDto model)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        
        if (userId is null)
        {
            return Unauthorized("User is not authenticated.");
        }

        var cart = await dbContext.Carts
            .Include(c => c.Items)
            .ThenInclude(ci => ci.Clothes)
            .FirstOrDefaultAsync(c => c.UserId == userId);

        if (cart == null)
        {
            cart = new Cart { UserId = userId };
            dbContext.Carts.Add(cart);
        }

        var product = await dbContext.Clothes.FindAsync(model.ProductId);
        if (product == null)
        {
            return NotFound("Product not found");
        }

        var cartItem = cart.Items.FirstOrDefault(ci => ci.ClothesId == product.Id);
        if (cartItem != null)
        {
            cartItem.Quantity += model.Quantity;
        }
        else
        {
            var newCartItem = new CartItem
            {
                CartId = cart.Id,
                ClothesId = product.Id,
                Quantity = model.Quantity,
                Clothes = product 
            };
            cart.Items.Add(newCartItem);
        }

        cart.TotalPrice = cart.Items.Sum(ci => ci.Quantity * ci.Clothes.Price);

        await dbContext.SaveChangesAsync();
        
        var cartDto = mapper.Map<CartDTO>(cart);

        var cacheKey = $"Cart_{userId}";
        await cacheService.SetRecordAsync(cacheKey, cartDto);
        
        return Ok(new CartResponseDto
        {
            IsSuccessfulCart = true,
            UpdatedTotalQuantity = cart.Items.Sum(ci => ci.Quantity)
        });
    }
    
    [HttpPut("update")]
    public async Task<IActionResult> UpdateItemQuantity(int itemId, [FromQuery] int quantity)
    {
        var cartItem = await dbContext.CartItems
            .Include(ci => ci.Cart)
            .ThenInclude(c => c.Items)
            .ThenInclude(i => i.Clothes)
            .Include(ci => ci.Clothes)
            .FirstOrDefaultAsync(ci => ci.Id == itemId);

        if (cartItem == null)
        {
            return NotFound("Cart item not found");
        }

        cartItem.Quantity = quantity > 0 ? quantity : throw new ArgumentException("Quantity must be greater than zero");
        cartItem.Cart.TotalPrice = cartItem.Cart.Items.Sum(ci => ci.Quantity * ci.Clothes.Price);

        await dbContext.SaveChangesAsync();

        var cacheKey = $"Cart_{cartItem.Cart.UserId}";
        var cartDto = mapper.Map<CartDTO>(cartItem.Cart);


        await cacheService.SetRecordAsync(cacheKey, cartDto);

        return Ok(new CartResponseDto
        {
            IsSuccessfulCart = true, 
            UpdatedItemTotalPrice = cartItem.Price, 
            UpdatedCartTotalPrice = cartItem.Cart.TotalPrice
        });
    }
    
    [HttpGet("total-quantity")]
    public async Task<IActionResult> GetTotalQuantity()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var cacheKey = $"Cart_{userId}";

        var cachedCart = await cacheService.GetRecordAsync<CartDTO>(cacheKey);

        if (cachedCart != null)
        {
            var totalQuantity = cachedCart.Items.Sum(i => i.Quantity);
            return Ok(totalQuantity);
        }

        var totalQuantityDb = await dbContext.CartItems
            .Include(ci => ci.Cart)
            .Where(ci => ci.Cart.UserId == userId)
            .SumAsync(ci => ci.Quantity);

        return Ok(totalQuantityDb);
    }
    
    [HttpDelete("remove/{itemId:int}")]
    public async Task<IActionResult> RemoveItem(int itemId)
    {
        var cartItem = await dbContext.CartItems
            .Include(ci => ci.Cart)
            .ThenInclude(c => c.Items)
            .ThenInclude(i => i.Clothes)
            .Include(ci => ci.Clothes)
            .FirstOrDefaultAsync(ci => ci.Id == itemId);


        if (cartItem == null)
        {
            return NotFound("Cart item not found");
        }

        var cart = cartItem.Cart;
        dbContext.CartItems.Remove(cartItem);
        cart.TotalPrice = cart.Items.Where(ci => ci.Id != itemId).Sum(ci => ci.Quantity * ci.Clothes.Price);

        await dbContext.SaveChangesAsync();

        var cacheKey = $"Cart_{cart.UserId}";
        var cartDto = mapper.Map<CartDTO>(cart);

        await cacheService.SetRecordAsync(cacheKey, cartDto);

        return Ok(new CartResponseDto { IsSuccessfulCart = true });
    }
    
    [HttpDelete("clear")]
    public async Task<IActionResult> ClearCart()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (userId is null)
        {
            return Unauthorized("User is not authenticated.");
        }

        var cart = await dbContext.Carts
            .Include(c => c.Items)
            .FirstOrDefaultAsync(c => c.UserId == userId);

        if (cart == null)
        {
            return NotFound("Cart not found");
        }

        dbContext.CartItems.RemoveRange(cart.Items);
        cart.Items.Clear();
        cart.TotalPrice = 0;

        await dbContext.SaveChangesAsync();

        var cacheKey = $"Cart_{userId}";
        await cacheService.RemoveRecordAsync(cacheKey);

        return Ok(new CartResponseDto { IsSuccessfulCart = true });
    }
}