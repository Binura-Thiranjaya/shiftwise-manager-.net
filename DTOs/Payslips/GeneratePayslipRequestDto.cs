using System.ComponentModel.DataAnnotations;

namespace TandTFuel.Api.DTOs.Payslips;

public class GeneratePayslipRequestDto
{
    [Required]
    public Guid EmployeeId { get; set; }

    [Required]
    public DateTime PeriodStart { get; set; }

    [Required]
    public DateTime PeriodEnd { get; set; }
}