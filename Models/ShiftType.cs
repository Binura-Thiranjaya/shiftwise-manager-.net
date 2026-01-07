using System.ComponentModel.DataAnnotations;

namespace TandTFuel.Api.Models;

public class ShiftType : BaseEntity
{
    [Key]
    public Guid Id { get; set; }

    [Required, MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Description { get; set; }

    public bool IsActive { get; set; } = true;

    public ICollection<EmployeeShift> Shifts { get; set; } = new List<EmployeeShift>();
}