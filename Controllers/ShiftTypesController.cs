using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TandTFuel.Api.Data;
using TandTFuel.Api.DTOs.ShiftTypes;
using TandTFuel.Api.Models;

namespace TandTFuel.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ShiftTypesController : ControllerBase
{
    private readonly AppDbContext _db;
    public ShiftTypesController(AppDbContext db) => _db = db;

    [Authorize]
    [HttpGet]
    public async Task<ActionResult<List<ShiftType>>> GetAll()
        => Ok(await _db.ShiftTypes.OrderBy(x => x.Name).ToListAsync());

    [Authorize(Policy = "ManagerOrAdmin")]
    [HttpPost]
    public async Task<ActionResult<ShiftType>> Create(ShiftTypeCreateDto dto)
    {
        var st = new ShiftType { Name = dto.Name.Trim(), Description = dto.Description, IsActive = true };
        _db.ShiftTypes.Add(st);
        await _db.SaveChangesAsync();
        return Ok(st);
    }

    [Authorize(Policy = "ManagerOrAdmin")]
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, ShiftTypeUpdateDto dto)
    {
        var st = await _db.ShiftTypes.FirstOrDefaultAsync(x => x.Id == id);
        if (st is null) return NotFound();

        st.Description = dto.Description;
        st.IsActive = dto.IsActive;

        await _db.SaveChangesAsync();
        return Ok(st);
    }
}