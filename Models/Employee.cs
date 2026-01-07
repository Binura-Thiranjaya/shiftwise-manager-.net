using System.ComponentModel.DataAnnotations;

namespace TandTFuel.Api.Models;

public class Employee : BaseEntity
{
    [Key]
    public Guid Id { get; set; }

    [Required, MaxLength(100)]
    public string FirstName { get; set; } = string.Empty;

    [Required, MaxLength(100)]
    public string LastName { get; set; } = string.Empty;

    [Required, MaxLength(255)]
    public string Email { get; set; } = string.Empty;

    [MaxLength(20)]
    public string? Phone { get; set; }

    [MaxLength(20)]
    public string? NINumber { get; set; }

    public decimal HourlyRateA { get; set; }
    public decimal HourlyRateB { get; set; }
    public decimal HoursForRateA { get; set; } = 40;

    public bool IsActive { get; set; } = true;

    public DateTime HireDate { get; set; }
    public DateTime? TerminationDate { get; set; }

    public ICollection<EmployeeStation> EmployeeStations { get; set; } = new List<EmployeeStation>();
    public ICollection<EmployeeShift> Shifts { get; set; } = new List<EmployeeShift>();
    public ICollection<Payslip> Payslips { get; set; } = new List<Payslip>();
}