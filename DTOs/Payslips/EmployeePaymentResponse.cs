namespace TandTFuel.Api.DTOs.Payslips;

public class EmployeePaymentResponse
{
    
    public Guid Id { get; set; }
    public DateTime PeriodStart { get; set; }
    public DateTime PeriodEnd { get; set; }
    public decimal TotalHours { get; set; }
    public decimal HoursRateA { get; set; }
    public decimal HoursRateB { get; set; }
    public decimal OvertimeHours { get; set; }
    public decimal GrossPay { get; set; }
    public decimal TaxDeduction { get; set; }
    public decimal NiDeduction { get; set; }
    public decimal OtherDeductions { get; set; }
    public decimal NetPay { get; set; }
    public string Status { get; set; } = "";
    public DateTime GeneratedAt { get; set; }
    
    
    
}