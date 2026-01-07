using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TandTFuel.Api.Models;

public class EmployeeShift : BaseEntity
{
    [Key]
    public Guid Id { get; set; }

    [Required]
    public Guid EmployeeId { get; set; }

    [ForeignKey(nameof(EmployeeId))]
    public Employee Employee { get; set; } = null!;

    [Required]
    public Guid StationId { get; set; }

    [ForeignKey(nameof(StationId))]
    public Station Station { get; set; } = null!;

    [Required]
    public Guid ShiftTypeId { get; set; }

    [ForeignKey(nameof(ShiftTypeId))]
    public ShiftType ShiftType { get; set; } = null!;

    [Required]
    public DateTime Date { get; set; }

    [Required]
    public TimeSpan TimeIn { get; set; }

    [Required]
    public TimeSpan TimeOut { get; set; }

    public decimal TotalHours { get; set; }
    public decimal HourlyRate { get; set; }

    [MaxLength(20)]
    public string Status { get; set; } = "Pending"; // Pending, Locked
    
    public DateTime? ApprovedAt { get; set; }

    [MaxLength(500)]
    public string? Notes { get; set; }
}