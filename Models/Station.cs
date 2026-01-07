using System.ComponentModel.DataAnnotations;

namespace TandTFuel.Api.Models;

public class Station : BaseEntity
{
    [Key]
    public Guid Id { get; set; }

    [Required, MaxLength(20)]
    public string Code { get; set; } = string.Empty;

    [Required, MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Location { get; set; }

    public bool IsActive { get; set; } = true;

    public ICollection<EmployeeStation> EmployeeStations { get; set; } = new List<EmployeeStation>();
    public ICollection<EmployeeShift> Shifts { get; set; } = new List<EmployeeShift>();
}