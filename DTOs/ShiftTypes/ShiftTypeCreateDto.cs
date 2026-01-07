using System.ComponentModel.DataAnnotations;

namespace TandTFuel.Api.DTOs.ShiftTypes;

public class ShiftTypeCreateDto
{
    [Required, MaxLength(100)]
    public string Name { get; set; } = "";

    [MaxLength(500)]
    public string? Description { get; set; }
}