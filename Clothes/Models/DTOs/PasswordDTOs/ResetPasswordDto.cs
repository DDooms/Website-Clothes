using System.ComponentModel.DataAnnotations;

namespace Clothes.Models.DTOs.PasswordDTOs;

public class ResetPasswordDto
{
    public required string? Password { get; set; }
    // [Compare("Password", ErrorMessage = "Passwords do not match")]
    public string? ConfirmPassword { get; set; }

    public string? Email { get; set; }
    public string? Token { get; set; }
}