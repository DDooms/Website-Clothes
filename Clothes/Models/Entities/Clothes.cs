using System.ComponentModel.DataAnnotations;
using Clothes.Models.Enums;

namespace Clothes.Models.Entities;

public class Clothes
{
    public int Id { get; set; }
    public required ClothingType Type { get; set; }
    public required Size Size { get; set; }
    [MaxLength(20, ErrorMessage = "Maximum length is 20 characters.")]
    public required string? Color { get; set; }
    public required decimal Price { get; set; }
    public DateTime DateAdded { get; set; } = DateTime.UtcNow;

    public DateTime? LastUpdated { get; set; }

    [MaxLength(15, ErrorMessage = "Maximum length is 15 characters.")]
    public string? Material { get; set; }
    public Gender? Gender { get; set; }
    [MaxLength(50, ErrorMessage = "Maximum length is 50 characters.")]
    public string? Description { get; set; }
    public string? ImageUrl { get; set; }
}