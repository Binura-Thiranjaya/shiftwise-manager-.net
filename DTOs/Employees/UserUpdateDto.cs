public class UserUpdateDto
{
    public string Email { get; set; } = "";
    public string FirstName { get; set; } = "";
    public string LastName { get; set; } = "";

    public string? Phone { get; set; }
    public string? NINumber { get; set; }

    public decimal HourlyRateA { get; set; }
    public decimal HourlyRateB { get; set; }
    public decimal HoursForRateA { get; set; }

    // Admin-only
    public string Role { get; set; } = "employee"; // admin/manager/supervisor/employee
    public bool IsActive { get; set; } = true;
}