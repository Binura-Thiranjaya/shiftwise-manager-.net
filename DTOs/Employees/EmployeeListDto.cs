namespace TandTFuel.Api.DTOs.Employees;

public class EmployeeListDto
{
    public Guid? Id { get; set; }  
    public Guid EmployeeId { get; set; }
    public string Role { get; set; }

    public string FirstName { get; set; } = "";
    public string LastName { get; set; } = "";
    public string Email { get; set; } = "";

    public string? Phone { get; set; }
    public string? NINumber { get; set; }

    public decimal HourlyRateA { get; set; }
    public decimal HourlyRateB { get; set; }
    public decimal HoursForRateA { get; set; }

    public bool IsActive { get; set; }
    public DateTime HireDate { get; set; }

    public List<EmployeeStationDto> Stations { get; set; } = new();
}

public class EmployeeStationDto
{
    public Guid StationId { get; set; }
    public string Code { get; set; } = "";
    public string Name { get; set; } = "";
    
}