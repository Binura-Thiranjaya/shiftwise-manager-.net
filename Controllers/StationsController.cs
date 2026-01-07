using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TandTFuel.Api.Data;
using TandTFuel.Api.DTOs.Stations;
using TandTFuel.Api.Models;

namespace TandTFuel.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class StationsController : ControllerBase
{
    private readonly AppDbContext _db;
    public StationsController(AppDbContext db) => _db = db;

    [Authorize(Policy = "ManagerOrAdmin")]
    [HttpGet]
    public async Task<ActionResult<List<Station>>> GetAll()
        => Ok(await _db.Stations.OrderBy(s => s.Code).ToListAsync());

    [Authorize(Policy = "ManagerOrAdmin")]
    [HttpPost]
    public async Task<ActionResult<Station>> Create(StationCreateDto dto)
    {
        var station = new Station { Code = dto.Code.Trim(), Name = dto.Name.Trim(), Location = dto.Location };
        _db.Stations.Add(station);
        await _db.SaveChangesAsync();
        return Ok(station);
    }

    [Authorize(Policy = "ManagerOrAdmin")]
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, StationUpdateDto dto)
    {
        var station = await _db.Stations.FirstOrDefaultAsync(x => x.Id == id);
        if (station is null) return NotFound();

        station.Name = dto.Name.Trim();
        station.Location = dto.Location;
        station.IsActive = dto.IsActive;

        await _db.SaveChangesAsync();
        return Ok(station);
    }
    //Get the stations assined to a users employee id get from the token
    [Authorize(Policy = "SupervisorOrAbove")]
    [HttpGet("my-stations")]
    public async Task<ActionResult<List<Station>>> GetAssignedStations()
    {
        var employeeIdClaim = User.FindFirstValue("employeeId");
        if (string.IsNullOrWhiteSpace(employeeIdClaim))
            return Forbid("This account is not linked to an employee.");
        if (!Guid.TryParse(employeeIdClaim, out var employeeId))
            return Unauthorized("Invalid employeeId claim.");
        var stations = await _db.EmployeeStations
            .Where(es => es.EmployeeId == employeeId)
            .Include(es => es.Station)
            .Select(es => es.Station)
            .ToListAsync();
        return Ok(stations);  
    }
    
}