namespace Clothes.Models.DTOs.CartDTOs;

public class CartItemDTO
{
    public int Id { get; set; } 
    public string? ClothingType { get; set; } 
    public string? Size { get; set; }
    public string? Color { get; set; }
    public string? Material { get; set; }
    public string? Gender { get; set; }
    public string? Description { get; set; }
    public int Quantity { get; set; } 
    public decimal TotalPrice { get; set; } 
    public string? ImageUrl { get; set; } 
}

