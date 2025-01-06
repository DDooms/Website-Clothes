namespace Clothes.Models.Entities;

public class CartItem
{
    public int Id { get; set; } // Unique identifier for the cart item
    public int CartId { get; set; } // Foreign key to Cart
    public Cart Cart { get; set; } // Navigation property to Cart
    public int ClothesId { get; set; } // Foreign key to the Clothes entity
    public Clothes Clothes { get; set; } // Navigation property to Clothes
    public int Quantity { get; set; } // Quantity of this item
    public decimal Price => Clothes.Price * Quantity; // Total price for this item
}

