using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TandTFuel.Api.Models;

public class PayslipDetail
{
    [Key]
    public Guid Id { get; set; }

    [Required]
    public Guid PayslipId { get; set; }

    [ForeignKey(nameof(PayslipId))]
    public Payslip Payslip { get; set; } = null!;

    public Guid? ShiftId { get; set; }

    [ForeignKey(nameof(ShiftId))]
    public EmployeeShift? Shift { get; set; }

    [Required]
    public DateTime Date { get; set; }

    public decimal Hours { get; set; }
    public decimal HourlyRate { get; set; }
    public decimal Amount { get; set; }

    [Required, MaxLength(200)]
    public string StationName { get; set; } = string.Empty;

    [Required, MaxLength(100)]
    public string ShiftType { get; set; } = string.Empty;
}