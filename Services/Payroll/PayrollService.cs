using Microsoft.EntityFrameworkCore;
using TandTFuel.Api.Data;
using TandTFuel.Api.Models;

namespace TandTFuel.Api.Services.Payroll;

public class PayrollService : IPayrollService
{
    private readonly AppDbContext _db;
    public PayrollService(AppDbContext db) => _db = db;

    public async Task<Payslip> GeneratePayslipAsync(Guid employeeId, DateTime periodStart, DateTime periodEnd)
    {
        var emp = await _db.Employees.FirstOrDefaultAsync(x => x.Id == employeeId);
        if (emp is null) throw new InvalidOperationException("Employee not found.");

        var shifts = await _db.EmployeeShifts
            .Include(s => s.Station)
            .Include(s => s.ShiftType)
            .Where(s => s.EmployeeId == employeeId
                        && s.Date.Date >= periodStart.Date
                        && s.Date.Date <= periodEnd.Date)
            .OrderBy(s => s.Date)
            .ToListAsync();

        var totalHours = shifts.Sum(s => s.TotalHours);
        var hoursA = Math.Min(totalHours, emp.HoursForRateA);
        var hoursB = Math.Max(0, totalHours - emp.HoursForRateA);

        var gross = (hoursA * emp.HourlyRateA) + (hoursB * emp.HourlyRateB);

        // Deductions (placeholder = 0; you can add tax/NI formulas later)
        var tax = 0m;
        var ni = 0m;
        var other = 0m;
        var net = gross - tax - ni - other;

        var payslip = new Payslip
        {
            EmployeeId = employeeId,
            PeriodStart = periodStart.Date,
            PeriodEnd = periodEnd.Date,
            TotalHours = totalHours,
            HoursAtRateA = hoursA,
            HoursAtRateB = hoursB,
            GrossPay = gross,
            TaxDeduction = tax,
            NIDeduction = ni,
            OtherDeductions = other,
            NetPay = net,
            Status = "Draft",
            GeneratedAt = DateTime.UtcNow,
            Details = shifts.Select(s => new PayslipDetail
            {
                ShiftId = s.Id,
                Date = s.Date.Date,
                Hours = s.TotalHours,
                HourlyRate = s.HourlyRate,
                Amount = s.TotalHours * s.HourlyRate,
                StationName = s.Station.Name,
                ShiftType = s.ShiftType.Name
            }).ToList()
        };

        _db.Payslips.Add(payslip);

        // lock shifts
        foreach (var s in shifts)
        {
            s.Status = "Locked";
        }

        await _db.SaveChangesAsync();
        return payslip;
    }
}
