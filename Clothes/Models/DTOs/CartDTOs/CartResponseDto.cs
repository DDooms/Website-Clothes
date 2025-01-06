namespace Clothes.Models.DTOs.CartDTOs;

public class CartResponseDto
{
    public bool IsSuccessfulCart { get; set; } = false;
    public IEnumerable<string>? Errors { get; set; }
    public decimal UpdatedItemTotalPrice { get; set; }
    public decimal UpdatedCartTotalPrice { get; set; }
    public int UpdatedTotalQuantity { get; set; }
}