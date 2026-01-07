using System.ComponentModel.DataAnnotations;

namespace TandTFuel.Api.DTOs.Shifts;

public class ShiftCreateDto
{
    [Required]
    public Guid EmployeeId { get; set; }

    [Required]
    public Guid StationId { get; set; }

    [Required]
    public Guid ShiftTypeId { get; set; }

    [Required]
    public DateTime Date { get; set; }

    [Required]
    public TimeSpan TimeIn { get; set; }

    [Required]
    public TimeSpan TimeOut { get; set; }

    public string? Notes { get; set; }
}