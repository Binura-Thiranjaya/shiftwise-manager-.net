namespace TandTFuel.Api.DTOs.Stations;

public class UpdateStationsDto
{
    public List<Guid> StationIds { get; set; } = new();
}
