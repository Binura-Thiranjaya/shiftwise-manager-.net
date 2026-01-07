using System.ComponentModel.DataAnnotations;

namespace TandTFuel.Api.DTOs.Employees;

public class CreateEmployeeDto
{
    [Required, MaxLength(100)]
    public string FirstName { get; set; } = string.Empty;

    [Required, MaxLength(100)]
    public string LastName { get; set; } = string.Empty;

    [Required, MaxLength(255)]
    public string Email { get; set; } = string.Empty;
    //Password will be generated and sent via email
    [Required, MinLength(6), MaxLength(100)]
    public string Password { get; set; } = string.Empty;
    //Role
    [Required]
    public string Role { get; set; } = "employee";
    
    [MaxLength(20)]
    public string? Phone { get; set; }

    [MaxLength(20)]
    public string? NINumber { get; set; }

    [Range(0, 999999)]
    public decimal HourlyRateA { get; set; }

    [Range(0, 999999)]
    public decimal HourlyRateB { get; set; }

    [Range(0, 168)]
    public decimal HoursForRateA { get; set; } = 40;

    [Required]
    public DateTime HireDate { get; set; }

    [Required, MinLength(1)]
    public List<Guid> StationIds { get; set; } = new();
}