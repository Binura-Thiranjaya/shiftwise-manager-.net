using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TandTFuel.Api.Data;
using TandTFuel.Api.DTOs.Payslips;
using TandTFuel.Api.Services.Payroll;

namespace TandTFuel.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PayslipsController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IPayrollService _payroll;

    public PayslipsController(AppDbContext db, IPayrollService payroll)
    {
        _db = db;
        _payroll = payroll;
    }

    [Authorize(Policy = "ManagerOrAdmin")]
    [HttpPost("generate")]
    public async Task<ActionResult<PayslipReadDto>> Generate(GeneratePayslipRequestDto dto)
    {
        if (dto.PeriodEnd.Date < dto.PeriodStart.Date)
            return BadRequest("PeriodEnd must be after PeriodStart.");

        var payslip = await _payroll.GeneratePayslipAsync(dto.EmployeeId, dto.PeriodStart, dto.PeriodEnd);

        return Ok(new PayslipReadDto
        {
            Id = payslip.Id,
            EmployeeId = payslip.EmployeeId,
            PeriodStart = payslip.PeriodStart,
            PeriodEnd = payslip.PeriodEnd,
            TotalHours = payslip.TotalHours,
            GrossPay = payslip.GrossPay,
            NetPay = payslip.NetPay,
            Status = payslip.Status,
            GeneratedAt = payslip.GeneratedAt
        });
    }

    [Authorize]
    [HttpGet("my")]
    public async Task<ActionResult<List<PayslipReadDto>>> My()
    {
        var employeeIdClaim = User.Claims.FirstOrDefault(c => c.Type == "employeeId")?.Value;
        if (string.IsNullOrWhiteSpace(employeeIdClaim)) return Forbid("Not an employee account.");
        if (!Guid.TryParse(employeeIdClaim, out var employeeId)) return Unauthorized("Invalid employeeId claim.");

        var list = await _db.Payslips
            .Where(p => p.EmployeeId == employeeId)
            .OrderByDescending(p => p.GeneratedAt)
            .Select(p => new PayslipReadDto
            {
                Id = p.Id,
                EmployeeId = p.EmployeeId,
                PeriodStart = p.PeriodStart,
                PeriodEnd = p.PeriodEnd,
                TotalHours = p.TotalHours,
                GrossPay = p.GrossPay,
                NetPay = p.NetPay,
                Status = p.Status,
                GeneratedAt = p.GeneratedAt
            })
            .ToListAsync();

        return Ok(list);
    }
    
    //NEED A FUNCTION TOTAL WHICH TAKE PARAMENTS FOR START DATE AND END DATE AND RETURNS TOTAL PAYSLIPS GENERATED IN THAT PERIOD
    [Authorize]
    [HttpGet("total")]
    public async Task<ActionResult<int>> Total(DateTime startDate, DateTime endDate)
    {
        if (endDate.Date < startDate.Date)
            return BadRequest("End date must be after start date.");
        var total = await _db.Payslips
            .Where(p => p.GeneratedAt.Date >= startDate.Date && p.GeneratedAt.Date <= endDate.Date)
            .CountAsync();
        return Ok(total);
    }
    
    //NEED TO GET THE HOW MANY HOURS WORKED PER WEEK FOR A GIVEN EMPLOYEE ID AND DATE RANGE AND RETURN HOURS A * RATE A AND REMAINING HOURS * RATE B
    [Authorize]
    [HttpGet("weekly-pay/{employeeId:guid}")]
    public async Task<IActionResult> GetWeeklyPay(
        Guid employeeId, 
        [FromQuery] DateTime startDate,
        [FromQuery] DateTime endDate)
    {
        // 1️⃣ Fetch employee info
        var employee = await _db.Employees.FirstOrDefaultAsync(e => e.Id == employeeId);
        if (employee == null) return NotFound("Employee not found.");

        // 2️⃣ Fetch shifts in date range
        var shifts = await _db.EmployeeShifts
            .Where(s => s.EmployeeId == employeeId 
                        && s.Date >= startDate 
                        && s.Date <= endDate 
                        && s.Status == "Locked") // Only locked shifts count for payroll
            .ToListAsync();

        if (!shifts.Any()) return Ok(new { message = "No worked hours in this range." });

        // 3️⃣ Aggregate total hours
        decimal totalHours = shifts.Sum(s => s.TotalHours);

        // 4️⃣ Calculate weekly pay
        decimal hoursForRateA = Math.Min(totalHours, employee.HoursForRateA);
        decimal hoursForRateB = Math.Max(0, totalHours - employee.HoursForRateA);

        decimal amountRateA = hoursForRateA * employee.HourlyRateA;
        decimal amountRateB = hoursForRateB * employee.HourlyRateB;

        decimal totalAmount = amountRateA + amountRateB;

        // 5️⃣ Return breakdown
        return Ok(new
        {
            EmployeeId = employeeId,
            TotalHours = totalHours,
            HoursRateA = hoursForRateA,
            HoursRateB = hoursForRateB,
            AmountRateA = amountRateA,
            AmountRateB = amountRateB,
            TotalAmount = totalAmount,
            ShiftCount = shifts.Count,
            WeekStart = startDate,
            WeekEnd = endDate
        });
    }

}
