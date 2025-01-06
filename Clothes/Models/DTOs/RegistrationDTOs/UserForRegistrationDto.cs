using System.ComponentModel.DataAnnotations;

namespace Clothes.Models.DTOs.RegistrationDTOs;

public class UserForRegistrationDto
{
    [MaxLength(20)]
    public string? FirstName { get; set; }
    [MaxLength(20)]
    public string? LastName { get; set; }
    
    public required string Email { get; set; }
    public required string Password { get; set; }
    [Compare("Password", ErrorMessage = "Passwords do not match")]
    public string? ConfirmPassword { get; set; } = null;

    public string? ClientUri { get; set; }
}