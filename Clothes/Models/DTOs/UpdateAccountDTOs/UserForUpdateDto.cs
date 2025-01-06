using System.ComponentModel.DataAnnotations;

namespace Clothes.Models.DTOs.UpdateAccountDTOs;

public class UserForUpdateDto
{
    [MaxLength(20)]
    public string? FirstName { get; set; }
    
    [MaxLength(20)]
    public string? LastName { get; set; }

    public DateOnly DateOfBirth { get; set; }
    
    [MaxLength(20)]
    public string? PhoneNumber { get; set; }
}