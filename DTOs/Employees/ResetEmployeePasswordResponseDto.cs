namespace TandTFuel.Api.DTOs.Employees;

public class ResetEmployeePasswordResponseDto
{
    public Guid EmployeeId { get; set; }
    public Guid UserId { get; set; }
    public string Email { get; set; } = "";
    public string TemporaryPassword { get; set; } = "";
    public bool MustChangePass { get; set; }
}