using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TandTFuel.Api.Data;
using TandTFuel.Api.DTOs.Shifts;
using TandTFuel.Api.Models;

namespace TandTFuel.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ShiftsController : ControllerBase
{
    private readonly AppDbContext _db;
    public ShiftsController(AppDbContext db) => _db = db;

    // Manager/Admin creates shifts for employees
    [Authorize(Policy = "SupervisorOrAbove")]
    [HttpPost]
    public async Task<ActionResult<ShiftReadDto>> Create(ShiftCreateDto dto)
    {
        var emp = await _db.Employees.FirstOrDefaultAsync(e => e.Id == dto.EmployeeId);
        if (emp is null) return BadRequest("Employee not found.");

        var station = await _db.Stations.FirstOrDefaultAsync(s => s.Id == dto.StationId);
        if (station is null) return BadRequest("Station not found.");

        var shiftType = await _db.ShiftTypes.FirstOrDefaultAsync(st => st.Id == dto.ShiftTypeId);
        if (shiftType is null) return BadRequest("Shift type not found.");

        if (dto.TimeOut <= dto.TimeIn)
            return BadRequest("TimeOut must be after TimeIn.");

        var hours = (decimal)(dto.TimeOut - dto.TimeIn).TotalHours;
        var rate = emp.HourlyRateA;

        var shift = new EmployeeShift
        {
            EmployeeId = dto.EmployeeId,
            StationId = dto.StationId,
            ShiftTypeId = dto.ShiftTypeId,
            Date = dto.Date.Date,
            TimeIn = dto.TimeIn,
            TimeOut = dto.TimeOut,
            TotalHours = Math.Round(hours, 2),
            HourlyRate = rate,
            Status = "Pending",
            Notes = dto.Notes
        };

        _db.EmployeeShifts.Add(shift);
        await _db.SaveChangesAsync();

        // ✅ Return DTO (safe for JSON)
        var readDto = new ShiftReadDto
        {
            Id = shift.Id,
            Date = shift.Date,
            TimeIn = shift.TimeIn,
            TimeOut = shift.TimeOut,
            TotalHours = shift.TotalHours,
            HourlyRate = shift.HourlyRate,
            Status = shift.Status,
            StationName = station.Name,
            ShiftTypeName = shiftType.Name,
            // If you want employee details:
            EmployeeId = emp.Id,
            EmployeeFirstName = emp.FirstName,
            EmployeeLastName = emp.LastName,
            EmployeeEmail = emp.Email
        };

        return Ok(readDto);
    }

   [Authorize(Policy = "SupervisorOrAbove")]
[HttpGet]
public async Task<ActionResult<List<ShiftReadDto>>> GetAll(
    [FromQuery] DateTime? from,
    [FromQuery] DateTime? to)
{
    // ✅ role claim in token is: http://schemas.microsoft.com/ws/2008/06/identity/claims/role
    var actorRole = User.FindFirstValue(ClaimTypes.Role) ?? "";
    var isAdmin = actorRole.Equals("admin", StringComparison.OrdinalIgnoreCase);

    // ✅ may be null for admin accounts (no employee linked)
    var employeeIdClaim = User.FindFirstValue("employeeId");

    // ✅ Base query
    var q = _db.EmployeeShifts
        .AsNoTracking()
        .Include(s => s.Employee)
        .Include(s => s.Station)
        .Include(s => s.ShiftType)
        .AsQueryable();

    // ✅ Date filters
    if (from.HasValue) q = q.Where(s => s.Date >= from.Value.Date);
    if (to.HasValue) q = q.Where(s => s.Date <= to.Value.Date);

    // -----------------------------
    // ADMIN with no employeeId => see all shifts
    // -----------------------------
    if (isAdmin && string.IsNullOrWhiteSpace(employeeIdClaim))
    {
        var all = await q
            .OrderByDescending(s => s.Date)
            .Select(s => new ShiftReadDto
            {
                Id = s.Id,
                Date = s.Date,
                TimeIn = s.TimeIn,
                TimeOut = s.TimeOut,
                TotalHours = s.TotalHours,
                HourlyRate = s.HourlyRate,
                Status = s.Status,

                EmployeeId = s.Employee.Id,
                EmployeeFirstName = s.Employee.FirstName,
                EmployeeLastName = s.Employee.LastName,
                EmployeeEmail = s.Employee.Email,

                StationId = s.Station.Id,
                StationName = s.Station.Name,

                ShiftTypeId = s.ShiftType.Id,
                ShiftTypeName = s.ShiftType.Name
            })
            .ToListAsync();

        return Ok(all);
    }

    // -----------------------------
    // NON-ADMIN => station-scoped
    // -----------------------------
    if (string.IsNullOrWhiteSpace(employeeIdClaim))
        return Forbid("Not an employee account (missing employeeId claim).");

    if (!Guid.TryParse(employeeIdClaim, out var actorEmployeeId))
        return Unauthorized("Invalid employeeId claim.");

    // ✅ Station scope of logged-in user: only active link + active station
    var myStationIds = await _db.EmployeeStations
        .AsNoTracking()
        .Where(es => es.EmployeeId == actorEmployeeId && es.IsActive && es.Station.IsActive)
        .Select(es => es.StationId)
        .Distinct()
        .ToListAsync();

    if (myStationIds.Count == 0)
        return Ok(new List<ShiftReadDto>());

    // ✅ Only shifts at stations inside myStationIds + station active
    q = q.Where(s =>
        myStationIds.Contains(s.StationId) &&
        s.Station.IsActive);

    var scoped = await q
        .OrderByDescending(s => s.Date)
        .Select(s => new ShiftReadDto
        {
            Id = s.Id,
            Date = s.Date,
            TimeIn = s.TimeIn,
            TimeOut = s.TimeOut,
            TotalHours = s.TotalHours,
            HourlyRate = s.HourlyRate,
            Status = s.Status,

            EmployeeId = s.Employee.Id,
            EmployeeFirstName = s.Employee.FirstName,
            EmployeeLastName = s.Employee.LastName,
            EmployeeEmail = s.Employee.Email,

            StationId = s.Station.Id,
            StationName = s.Station.Name,

            ShiftTypeId = s.ShiftType.Id,
            ShiftTypeName = s.ShiftType.Name
        })
        .ToListAsync();

    return Ok(scoped);
}

    // Employee view own shifts
    [Authorize]
    [HttpGet("my")]
    public async Task<ActionResult<List<ShiftReadDto>>> My([FromQuery] DateTime? from, [FromQuery] DateTime? to)
    {
        var employeeIdClaim = User.FindFirstValue("employeeId");
        if (string.IsNullOrWhiteSpace(employeeIdClaim))
            return Forbid("Not an employee account.");

        if (!Guid.TryParse(employeeIdClaim, out var employeeId))
            return Unauthorized("Invalid employeeId claim.");

        var q = _db.EmployeeShifts
            .AsNoTracking()
            .Include(s => s.Station)
            .Include(s => s.ShiftType)
            .Where(s => s.EmployeeId == employeeId);

        if (from.HasValue) q = q.Where(s => s.Date >= from.Value.Date);
        if (to.HasValue) q = q.Where(s => s.Date <= to.Value.Date);

        var list = await q
            .OrderByDescending(s => s.Date)
            .Select(s => new ShiftReadDto
            {
                Id = s.Id,
                EmployeeId = s.EmployeeId,
                Date = s.Date,
                TimeIn = s.TimeIn,
                TimeOut = s.TimeOut,
                TotalHours = s.TotalHours,
                HourlyRate = s.HourlyRate,
                Status = s.Status,
                Notes = s.Notes,
                StationId = s.StationId,
                StationName = s.Station.Name,
                ShiftTypeId = s.ShiftTypeId,
                ShiftTypeName = s.ShiftType.Name
            })
            .ToListAsync();

        return Ok(list);
    }
    
    //Togggle shift status
   [Authorize]
    [HttpPut("{id:guid}/status")]
    public async Task<IActionResult> UpdateStatus(Guid id, [FromBody] ShiftStatusUpdate dto)
    {
        // ✅ Start transaction (rollback everything if any error)
        await using var tx = await _db.Database.BeginTransactionAsync();

        try
        {
            var shift = await _db.EmployeeShifts.FirstOrDefaultAsync(s => s.Id == id);
            if (shift is null) return NotFound("Shift not found.");

            // Normalize
            var current = (shift.Status ?? "").Trim();
            var next = (dto.Status ?? "").Trim();

            if (string.IsNullOrWhiteSpace(next))
                return BadRequest("Status is required.");

            // ✅ Only allow these statuses
            var allowed = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "Pending", "Approved", "Locked", "Rejected"
            };

            if (!allowed.Contains(next))
                return BadRequest("Invalid status value. Allowed: Pending, Approved, Locked, Rejected.");

            // ✅ Final states cannot be modified
            if (current.Equals("Locked", StringComparison.OrdinalIgnoreCase))
                return BadRequest("Locked shift cannot be modified.");

            if (current.Equals("Rejected", StringComparison.OrdinalIgnoreCase))
                return BadRequest("Rejected shift cannot be modified.");

            // ✅ Helper role check (safe for casing)
            bool IsInAnyRole(params string[] roles)
                => roles.Any(r =>
                    User.IsInRole(r) ||
                    User.IsInRole(r.ToLower()) ||
                    User.IsInRole(r.ToUpper()));

            var isSupervisorOrAbove = IsInAnyRole("Supervisor", "Manager", "Admin");

            // ✅ Determine if caller is employee (has employeeId claim)
            var employeeIdClaim = User.FindFirstValue("employeeId");
            var hasEmployeeId = Guid.TryParse(employeeIdClaim, out var callerEmployeeId);

            // ✅ If NOT supervisor/admin, must be a valid employee and must own the shift
            if (!isSupervisorOrAbove)
            {
                if (!hasEmployeeId)
                    return Forbid("Not an employee account.");

                if (shift.EmployeeId != callerEmployeeId)
                    return Forbid("Cannot change status of another employee's shift.");

                // ✅ Employee can only move Pending -> Approved/Rejected
                if (!current.Equals("Pending", StringComparison.OrdinalIgnoreCase))
                    return BadRequest("Employees can only update Pending shifts.");

                if (!(next.Equals("Approved", StringComparison.OrdinalIgnoreCase) ||
                      next.Equals("Rejected", StringComparison.OrdinalIgnoreCase)))
                    return BadRequest("Employees can only set status to Approved or Rejected.");
            }

            // ✅ Supervisor/Admin rules
            // - Can approve/reject pending
            // - Can lock only Approved
            if (next.Equals("Locked", StringComparison.OrdinalIgnoreCase) &&
                !current.Equals("Approved", StringComparison.OrdinalIgnoreCase))
            {
                return BadRequest("Only Approved shifts can be Locked.");
            }

            // ✅ Apply status change
            shift.Status = next;

            // ✅ timestamps
            if (next.Equals("Approved", StringComparison.OrdinalIgnoreCase) ||
                next.Equals("Rejected", StringComparison.OrdinalIgnoreCase))
            {
                shift.ApprovedAt = DateTime.UtcNow; // keep your column (even though name says ApprovedAt)
            }

            // If you have these columns, uncomment:
            // if (next.Equals("Locked", StringComparison.OrdinalIgnoreCase))
            // {
            //     shift.LockedAt = DateTime.UtcNow;
            //     shift.IsLocked = true;
            // }

            // ✅ When LOCKED → create/update payslip for payroll processing
            if (next.Equals("Locked", StringComparison.OrdinalIgnoreCase))
            {
                // ---- Period logic (simple DAILY payslip; safe default) ----
                // If you want weekly/monthly, tell me and I’ll adjust.
                var periodStart = shift.Date.Date;
                var periodEnd = shift.Date.Date;

                // ✅ Find existing payslip for this employee+period (avoid duplicates)
                var payslip = await _db.Payslips.FirstOrDefaultAsync(p =>
                    p.EmployeeId == shift.EmployeeId &&
                    p.PeriodStart == periodStart &&
                    p.PeriodEnd == periodEnd &&
                    p.Status != "Voided");

                var shiftHours = shift.TotalHours;
                var shiftGross = Math.Round(shift.TotalHours * shift.HourlyRate, 2);
                var employeePay = _db.Employees.FirstOrDefault(e => e.Id == shift.EmployeeId);
                
                if (payslip == null)
                {
                    //In the employee db Weely Hours for rate A is thier and need to cal culate by perhour rate and rest from rate B and store in paydetails
                    payslip = new Payslip
                    {
                        Id = Guid.NewGuid(),
                        EmployeeId = shift.EmployeeId,
                        PeriodStart = periodStart,
                        PeriodEnd = periodEnd,
                        TotalHours = shiftHours,
                        
                        GrossPay = shiftGross,
                        NetPay = shiftGross, // adjust later for deductions
                        Status = "Generated",
                        GeneratedAt = DateTime.UtcNow
                    };

                    _db.Payslips.Add(payslip);
                }
                else
                {
                    // ✅ Update totals (add this locked shift)
                    payslip.TotalHours = Math.Round((payslip.TotalHours + shiftHours), 2);
                    payslip.GrossPay = Math.Round((payslip.GrossPay + shiftGross), 2);
                    payslip.NetPay = Math.Round((payslip.NetPay + shiftGross), 2);

                    // keep status as Generated or whatever you use
                    if (string.IsNullOrWhiteSpace(payslip.Status))
                        payslip.Status = "Generated";
                }
            }

            await _db.SaveChangesAsync();
            await tx.CommitAsync();

            return Ok(new { message = $"Shift {next} successfully." });
        }
        catch (Exception ex)
        {
            await tx.RollbackAsync();
            // ✅ Return clean error for frontend (JSON)
            return StatusCode(500, new { message = "Failed to update shift status.", error = ex.Message });
        }
    }


        
}
