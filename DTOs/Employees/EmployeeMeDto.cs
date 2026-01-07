namespace TandTFuel.Api.DTOs.Employees;

public class EmployeeMeDto
{
    public Guid EmployeeId { get; set; }
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
}