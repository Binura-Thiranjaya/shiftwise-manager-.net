using System.ComponentModel.DataAnnotations;

namespace TandTFuel.Api.DTOs.ShiftTypes;

public class ShiftTypeUpdateDto
{
    [Required, MaxLength(500)]
    public string? Description { get; set; }

    public bool IsActive { get; set; } = true;
}