namespace Clothes.Models.Entities;

public class Cart
{
    public int Id { get; set; }
    public string UserId { get; set; } // Foreign key to the User
    public ICollection<CartItem> Items { get; set; } = new List<CartItem>();
    public decimal TotalPrice { get; set; }
}