using System.ComponentModel.DataAnnotations;

namespace TandTFuel.Api.DTOs.Stations;

public class StationCreateDto
{
    [Required, MaxLength(20)]
    public string Code { get; set; } = "";

    [Required, MaxLength(200)]
    public string Name { get; set; } = "";

    [MaxLength(500)]
    public string? Location { get; set; }
}