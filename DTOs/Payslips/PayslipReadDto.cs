namespace TandTFuel.Api.DTOs.Payslips;

public class PayslipReadDto
{
    public Guid Id { get; set; }
    public Guid EmployeeId { get; set; }
    public DateTime PeriodStart { get; set; }
    public DateTime PeriodEnd { get; set; }
    public decimal TotalHours { get; set; }
    public decimal GrossPay { get; set; }
    public decimal NetPay { get; set; }
    public string Status { get; set; } = "";
    public DateTime GeneratedAt { get; set; }
}