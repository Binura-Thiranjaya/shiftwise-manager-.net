using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TandTFuel.Api.Models;

public class EmployeeStation
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

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}