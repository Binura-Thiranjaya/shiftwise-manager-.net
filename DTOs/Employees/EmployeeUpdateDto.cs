public class EmployeeUpdateDto
{
    // Account
    public string Email { get; set; } = "";
    public string? Role { get; set; }          // admin-only
    public bool? IsActive { get; set; }        // admin-only

    // Employee
    public string FirstName { get; set; } = "";
    public string LastName { get; set; } = "";
    public string? Phone { get; set; }
    public string? NINumber { get; set; }
    public decimal HourlyRateA { get; set; }
    public decimal HourlyRateB { get; set; }
    public decimal HoursForRateA { get; set; }

    // Stations (admin-only)
    public List<Guid>? StationIds { get; set; }

    // 🔐 Password (optional)
    public string? NewPassword { get; set; }
}