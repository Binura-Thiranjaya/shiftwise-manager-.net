namespace TandTFuel.Api.DTOs.Employees;

public class UserWithEmployeeDto
{
    public Guid Id { get; set; }
    public string Email { get; set; } = "";
    public string Role { get; set; } = "";
    public bool IsActive { get; set; }
    public Guid? EmployeeId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? LastLoginAt { get; set; }

    // Employee info (nullable)
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? Phone { get; set; }
    public string? NINumber { get; set; }
    public decimal? HourlyRateA { get; set; }
    public decimal? HourlyRateB { get; set; }
    public decimal? HoursForRateA { get; set; }
    public DateTime? HireDate { get; set; }
}