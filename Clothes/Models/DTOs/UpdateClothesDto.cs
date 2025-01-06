using Clothes.Models.Enums;

namespace Clothes.Models.DTOs;

public class UpdateClothesDto
{
    public ClothingType? Type { get; set; }
    public Size? Size { get; set; }
    public string? Color { get; set; }
    public decimal? Price { get; set; }
    public DateTime? LastUpdated { get; set; }

    public string? Material { get; set; }
    public Gender? Gender { get; set; }
    public string? Description { get; set; }
    public IFormFile? ImageUrl { get; set; }
}
