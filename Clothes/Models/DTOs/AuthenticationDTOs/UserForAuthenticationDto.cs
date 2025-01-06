namespace Clothes.Models.DTOs.AuthenticationDTOs;

public class UserForAuthenticationDto
{
    public required string? Email { get; set; }
    public required string? Password { get; set; }
}