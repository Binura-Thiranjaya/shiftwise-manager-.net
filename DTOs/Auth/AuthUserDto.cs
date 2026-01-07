namespace TandTFuel.Api.DTOs.Auth;

public class AuthUserDto
{
    public Guid Id { get; set; }
    public string Email { get; set; } = "";
    public string Role { get; set; } = "";
    public Guid? EmployeeId { get; set; }
    public bool MustChangePass { get; set; }
}