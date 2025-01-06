using AutoMapper;
using Clothes.Data;
using Clothes.Models.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Clothes.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ClothesController : ControllerBase
{
    private readonly ApplicationDbContext dbContext;
    private readonly IMapper mapper;
    private readonly RedisCacheService cacheService;
    
    public ClothesController(ApplicationDbContext dbContext, IMapper mapper, RedisCacheService cacheService)
    {
        this.dbContext = dbContext;
        this.mapper = mapper;
        this.cacheService = cacheService;
    }
    
    [HttpGet]
    public async Task<IActionResult> GetClothes([FromQuery] string? searchValue)
    {
        var cacheKey = string.IsNullOrEmpty(searchValue) 
            ? "Clothes_All" 
            : $"Clothes_Search_{searchValue}";

        var cachedClothes = await cacheService.GetRecordAsync<List<Models.Entities.Clothes>>(cacheKey);
        if (cachedClothes != null)
        {
            return Ok(cachedClothes);
        }

        IQueryable<Models.Entities.Clothes> query = dbContext.Clothes;

        if (!string.IsNullOrEmpty(searchValue))
        {
            query = query.Where(c => c.Type.ToString().Contains(searchValue));
        }

        var clothes = query.ToList();

        if (clothes.Count == 0)
        {
            return NoContent();
        }

        await cacheService.SetRecordAsync(cacheKey, clothes);

        return Ok(clothes);
    }
    
    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> AddClothes([FromForm] AddClothesDto addClothesDto)
    {
        string? relativePath = null;

        if (addClothesDto.ImageUrl is { Length: > 0 })
        {
            relativePath = await SaveImageAsync(addClothesDto.ImageUrl);
        }

        var clothes = mapper.Map<Models.Entities.Clothes>(addClothesDto);
        clothes.ImageUrl = relativePath;

        dbContext.Clothes.Add(clothes);
        await dbContext.SaveChangesAsync();
        await cacheService.RemoveRecordAsync("Clothes_All");
        
        return Created("", clothes);
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> UpdateClothesById(int id, [FromForm] UpdateClothesDto updateClothesDto)
    {
        var clothes = await dbContext.Clothes.FindAsync(id);

        if (clothes is null)
        {
            return NotFound();
        }

        if (updateClothesDto.ImageUrl is { Length: > 0 })
        {
            clothes.ImageUrl = await SaveImageAsync(updateClothesDto.ImageUrl);
        }

        mapper.Map(updateClothesDto, clothes);
        clothes.LastUpdated = DateTime.UtcNow;

        await dbContext.SaveChangesAsync();
        await cacheService.RemoveRecordAsync("Clothes_All");
        return Ok(clothes);
    }
    
    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeleteClothesById(int id)
    {
        var clothes = await dbContext.Clothes.FindAsync(id);
        if (clothes is null)
        {
            return NotFound();
        }
        
        dbContext.Clothes.Remove(clothes);
        await dbContext.SaveChangesAsync();
        await cacheService.RemoveRecordAsync("Clothes_All");
        
        return Ok();
    }
    
    private static async Task<string> SaveImageAsync(IFormFile imageFile)
    {
        var fileName = Guid.NewGuid() + Path.GetExtension(imageFile.FileName);
        var filePath = Path.Combine("Resources", "Images", fileName);

        await using var stream = new FileStream(filePath, FileMode.Create);
        await imageFile.CopyToAsync(stream);

        return "Images/" + fileName;
    }
}