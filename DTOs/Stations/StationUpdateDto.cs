using System.ComponentModel.DataAnnotations;

namespace TandTFuel.Api.DTOs.Stations;

public class StationUpdateDto
{
    [Required, MaxLength(200)]
    public string Name { get; set; } = "";

    [MaxLength(500)]
    public string? Location { get; set; }

    public bool IsActive { get; set; } = true;
}