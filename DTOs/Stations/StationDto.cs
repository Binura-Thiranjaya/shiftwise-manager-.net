namespace TandTFuel.Api.DTOs.Stations;

public class StationDto
{
    public Guid StationId { get; set; }

    public string Code { get; set; } = null!;

    public string Name { get; set; } = null!;

    public string? Location { get; set; }

    public bool IsActive { get; set; }

}