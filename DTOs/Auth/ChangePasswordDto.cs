using System.ComponentModel.DataAnnotations;

namespace TandTFuel.Api.DTOs.Auth;

public class ChangePasswordDto
{
    [Required, MinLength(6), MaxLength(100)]
    public string CurrentPassword { get; set; } = string.Empty;

    [Required, MinLength(6), MaxLength(100)]
    public string NewPassword { get; set; } = string.Empty;
}