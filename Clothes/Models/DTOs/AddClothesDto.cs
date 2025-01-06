using Clothes.Models.Enums;

namespace Clothes.Models.DTOs;

public class AddClothesDto
{
    public ClothingType Type { get; set; }
    public Size Size { get; set; }
    public string? Color { get; set; }
    public decimal Price { get; set; }
    public DateTime DateAdded { get; set; } = DateTime.UtcNow;

    public string? Material { get; set; }
    public Gender? Gender { get; set; }
    public string? Description { get; set; }
    public IFormFile? ImageUrl { get; set; }
}