public class ShiftReadDto
{
    public Guid Id { get; set; }

    public DateTime Date { get; set; }
    public TimeSpan TimeIn { get; set; }
    public TimeSpan TimeOut { get; set; }

    public decimal TotalHours { get; set; }
    public decimal HourlyRate { get; set; }
    public string Status { get; set; } = "";
    public bool IsLocked { get; set; }
    public string? Notes { get; set; }

    // ✅ Employee
    public Guid EmployeeId { get; set; }
    public string EmployeeFirstName { get; set; } = "";
    public string EmployeeLastName { get; set; } = "";
    public string EmployeeEmail { get; set; } = "";

    // ✅ Station / ShiftType
    public Guid StationId { get; set; }
    public string StationName { get; set; } = "";

    public Guid ShiftTypeId { get; set; }
    public string ShiftTypeName { get; set; } = "";
}