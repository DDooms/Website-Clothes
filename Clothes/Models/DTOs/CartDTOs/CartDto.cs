namespace Clothes.Models.DTOs.CartDTOs;

public class CartDTO
{
    public int Id { get; set; } // Cart ID
    public List<CartItemDTO> Items { get; set; } = new(); // List of items in the cart
    public decimal TotalPrice => Items.Sum(item => item.TotalPrice); // Sum of all item prices
}

