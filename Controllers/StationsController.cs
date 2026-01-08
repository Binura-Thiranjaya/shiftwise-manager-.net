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

    //Get the assigned stations for a login user if the IT Admin login then get all stations to check is itdmin or not , in employeeid is not their
    [Authorize( Policy = "SupervisorOrAbove")]
    [HttpGet]
    public async Task<ActionResult<List<Station>>> GetAll()
    {
        var employeeIdClaim = User.FindFirstValue("employeeId");
        if (string.IsNullOrWhiteSpace(employeeIdClaim))
        {
            // IT Admin case - return all stations
            var allStations = await _db.Stations.OrderBy(s => s.Name).ToListAsync();
            return Ok(allStations);
        }
        if (!Guid.TryParse(employeeIdClaim, out var employeeId))
            return Unauthorized("Invalid employeeId claim.");
        var stations = await _db.EmployeeStations
            .Where(es => es.EmployeeId == employeeId && es.IsActive)
            .Include(es => es.Station)
            .Select(es => es.Station)
            .OrderBy(s => s.Name)
            .ToListAsync();
       
        
        return Ok(stations);
    }
    
                    
    
                    
                    
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
        station.Code = dto.Code.Trim();
        station.Name = dto.Name.Trim();
        station.Location = dto.Location;
        station.IsActive = dto.IsActive;

        await _db.SaveChangesAsync();
        return Ok(station);
    }
   
    //Patch toggle-status
    [Authorize(Policy = "ManagerOrAdmin")]
    [HttpPatch("{id:guid}/toggle-status")]
    public async Task<IActionResult> ToggleStatus(Guid id)
    {
        var station = await _db.Stations.FirstOrDefaultAsync(x => x.Id == id );
        if (station is null) return NotFound();
        station.IsActive = !station.IsActive;
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