using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TandTFuel.Api.Models;

public class Payslip : BaseEntity
{
    [Key]
    public Guid Id { get; set; }

    [Required]
    public Guid EmployeeId { get; set; }

    [ForeignKey(nameof(EmployeeId))]
    public Employee Employee { get; set; } = null!;

    [Required]
    public DateTime PeriodStart { get; set; }

    [Required]
    public DateTime PeriodEnd { get; set; }

    public decimal TotalHours { get; set; }
    public decimal HoursAtRateA { get; set; }
    public decimal HoursAtRateB { get; set; }

    public decimal GrossPay { get; set; }
    public decimal TaxDeduction { get; set; }
    public decimal NIDeduction { get; set; }
    public decimal OtherDeductions { get; set; }
    public decimal NetPay { get; set; }

    [Required, MaxLength(20)]
    public string Status { get; set; } = "Draft"; // Draft, Approved, Paid

    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ApprovedAt { get; set; }
    public DateTime? PaidAt { get; set; }

    public ICollection<PayslipDetail> Details { get; set; } = new List<PayslipDetail>();
}