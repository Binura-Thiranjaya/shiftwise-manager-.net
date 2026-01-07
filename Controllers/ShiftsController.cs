using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
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
            IsLocked = false,
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
        var q = _db.EmployeeShifts
            .Include(s => s.Employee)   // ✅ IMPORTANT
            .Include(s => s.Station)
            .Include(s => s.ShiftType)
            .AsQueryable();

        if (from.HasValue)
            q = q.Where(s => s.Date >= from.Value.Date);

        if (to.HasValue)
            q = q.Where(s => s.Date <= to.Value.Date);

        var list = await q
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

                // ✅ Employee details
                EmployeeId = s.Employee.Id,
                EmployeeFirstName = s.Employee.FirstName,
                EmployeeLastName = s.Employee.LastName,
                EmployeeEmail = s.Employee.Email,

                // ✅ Station
                StationId = s.Station.Id,
                StationName = s.Station.Name,

                // ✅ Shift type
                ShiftTypeId = s.ShiftType.Id,
                ShiftTypeName = s.ShiftType.Name
            })
            .ToListAsync();

        return Ok(list);
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
                IsLocked = s.IsLocked,
                Notes = s.Notes,
                StationId = s.StationId,
                StationName = s.Station.Name,
                ShiftTypeId = s.ShiftTypeId,
                ShiftTypeName = s.ShiftType.Name
            })
            .ToListAsync();

        return Ok(list);
    }
}
