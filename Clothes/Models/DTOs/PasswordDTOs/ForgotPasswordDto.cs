using System.ComponentModel.DataAnnotations;

namespace Clothes.Models.DTOs.PasswordDTOs;

public class ForgotPasswordDto
{
    [EmailAddress]
    public required string? Email { get; set; }
}