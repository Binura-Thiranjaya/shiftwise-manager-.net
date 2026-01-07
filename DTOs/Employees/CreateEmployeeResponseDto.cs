namespace TandTFuel.Api.DTOs.Employees;

public class CreateEmployeeResponseDto
{
    public Guid EmployeeId { get; set; }
    public Guid UserId { get; set; }

    public string Email { get; set; } = "";
    public string Role { get; set; } = "employee";

    // show only once
    public string TemporaryPassword { get; set; } = "";

    public List<AssignedStationDto> AssignedStations { get; set; } = new();
}

public class AssignedStationDto
{
    public Guid StationId { get; set; }
    public string Code { get; set; } = "";
    public string Name { get; set; } = "";
}