using Microsoft.AspNetCore.Identity;

namespace Clothes.Models.Entities;

public class Role : IdentityRole
{
    public string? Description { get; set; }
}