using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;

namespace Clothes.Models.Entities;

public class User : IdentityUser
{
    [MaxLength(20)]
    public required string FirstName { get; set; }
    [MaxLength(20)]
    public required string LastName { get; set; }
    public DateOnly DateOfBirth { get; set; }
    public string? RefreshToken { get; set; }
    public DateTime RefreshTokenExpiryTime { get; set; }
}