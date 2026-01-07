using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TandTFuel.Api.Data;
using TandTFuel.Api.DTOs.Employees;
using TandTFuel.Api.DTOs.Stations;
using TandTFuel.Api.Models;
using TandTFuel.Api.Services.Passwords;

namespace TandTFuel.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class EmployeesController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IPasswordHasher _hasher;

    public EmployeesController(AppDbContext db, IPasswordHasher hasher)
    {
        _db = db;
        _hasher = hasher;
    }

    // ✅ Employee can see their own profile
    [Authorize]
    [HttpGet("me")]
    public async Task<ActionResult<EmployeeMeDto>> Me()
    {
        var employeeIdClaim = User.FindFirstValue("employeeId");
        if (string.IsNullOrWhiteSpace(employeeIdClaim))
            return Forbid("This account is not linked to an employee.");

        if (!Guid.TryParse(employeeIdClaim, out var employeeId))
            return Unauthorized("Invalid employeeId claim.");

        var emp = await _db.Employees.FirstOrDefaultAsync(x => x.Id == employeeId);
        if (emp is null) return NotFound("Employee not found.");

        return Ok(new EmployeeMeDto
        {
            EmployeeId = emp.Id,
            FirstName = emp.FirstName,
            LastName = emp.LastName,
            Email = emp.Email,
            Phone = emp.Phone,
            NINumber = emp.NINumber,
            HourlyRateA = emp.HourlyRateA,
            HourlyRateB = emp.HourlyRateB,
            HoursForRateA = emp.HoursForRateA,
            IsActive = emp.IsActive,
            HireDate = emp.HireDate
        });
    }
    // ✅ Get Employee by Id with Stations[Authorize(Policy = "ManagerOrAdmin")]
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<EmployeeListDto>> GetById(Guid id)
    { 
        var emp = await
            (from e in _db.Employees.AsNoTracking()
                join u in _db.Users.AsNoTracking()
                    on e.Id equals u.EmployeeId into userJoin
                from u in userJoin.DefaultIfEmpty()
                where u.Id == id
                select new EmployeeListDto
                {
                    EmployeeId = e.Id,
                    Id = u != null ? u.Id : null,

                    FirstName = e.FirstName,
                    LastName = e.LastName,
                    Email = e.Email,
                    Phone = e.Phone,
                    NINumber = e.NINumber,
                    HourlyRateA = e.HourlyRateA,
                    HourlyRateB = e.HourlyRateB,
                    HoursForRateA = e.HoursForRateA,
                    IsActive = e.IsActive,
                    HireDate = e.HireDate,

                    Stations = e.EmployeeStations
                        .Select(es => new EmployeeStationDto
                        {
                            StationId = es.Station.Id,
                            Code = es.Station.Code,
                            Name = es.Station.Name
                        })
                        .ToList()
                })
            .FirstOrDefaultAsync();

        if (emp is null) return NotFound("Employee not found.");
        return Ok(emp);
    }

    //getStationsForEmployee
    [Authorize(Policy = "SupervisorOrAbove")]
    [HttpGet("{id:guid}/stations")]
    public async Task<ActionResult<List<StationDto>>> GetStationsForEmployee(Guid id)
    {
        var stations = await _db.EmployeeStations 
            .Where(es => es.EmployeeId == id)
            .Include(es => es.Station)
            .Select(es => new StationDto
            {
                StationId = es.Station.Id,
                Code = es.Station.Code,
                Name = es.Station.Name,
                Location = es.Station.Location,
                IsActive = es.Station.IsActive
            })
            .ToListAsync();
        return Ok(stations);
    }
    
    // ✅ Admin/Manager creates Employee + assigns Stations + creates login User
    [Authorize(Policy = "ManagerOrAdmin")]
    [HttpPost]
    public async Task<ActionResult<CreateEmployeeResponseDto>> Create([FromBody] CreateEmployeeDto dto)
    {
        var email = dto.Email.Trim().ToLower();
        var password = dto.Password.Trim();
        var role = dto.Role.Trim().ToLower();

        Console.WriteLine(password);
        // 1) Validate email uniqueness
        if (await _db.Employees.AnyAsync(x => x.Email.ToLower() == email))
            return BadRequest("Employee email already exists.");

        if (await _db.Users.AnyAsync(x => x.Email.ToLower() == email))
            return BadRequest("A user with this email already exists.");

        // 2) Validate stations exist and active
        var stationIds = dto.StationIds.Distinct().ToList();

        var stations = await _db.Stations
            .Where(s => stationIds.Contains(s.Id) && s.IsActive)
            .ToListAsync();

        if (stations.Count != stationIds.Count)
            return BadRequest("One or more StationIds are invalid or inactive.");

        using var tx = await _db.Database.BeginTransactionAsync();

        try
        {
            // 3) Create employee
            var employee = new Employee
            {
                FirstName = dto.FirstName.Trim(),
                LastName = dto.LastName.Trim(),
                Email = email,
                Phone = dto.Phone?.Trim(),
                NINumber = dto.NINumber?.Trim(),
                HourlyRateA = dto.HourlyRateA,
                HourlyRateB = dto.HourlyRateB,
                HoursForRateA = dto.HoursForRateA,
                HireDate = dto.HireDate.Date,
                IsActive = true
            };

            _db.Employees.Add(employee);
            await _db.SaveChangesAsync();

            // 4) Create linked user login
            var tempPassword = GenerateTempPassword();
            var user = new User
            {
                Email = email,
                PasswordHash = _hasher.Hash(password),
                Role = role.ToLower(),
                IsActive = true,
                MustChangePass = true,
                EmployeeId = employee.Id
            };

            _db.Users.Add(user);
            await _db.SaveChangesAsync();

            // 5) Assign stations (for-loop)
            for (var i = 0; i < stationIds.Count; i++)
            {
                _db.EmployeeStations.Add(new EmployeeStation
                {
                    EmployeeId = employee.Id,
                    StationId = stationIds[i],
                    CreatedAt = DateTime.UtcNow
                });
            }

            await _db.SaveChangesAsync();
            await tx.CommitAsync();

            // response
            var response = new CreateEmployeeResponseDto
            {
                EmployeeId = employee.Id,
                UserId = user.Id,
                Email = user.Email,
                Role = user.Role,
                TemporaryPassword = tempPassword
            };

            for (var i = 0; i < stations.Count; i++)
            {
                response.AssignedStations.Add(new AssignedStationDto
                {
                    StationId = stations[i].Id,
                    Code = stations[i].Code,
                    Name = stations[i].Name
                });
            }

            return Ok(response);
        }
        catch
        {
            await tx.RollbackAsync();
            throw;
        }
    }

    // ✅ Get all employees with stations
    [Authorize(Policy = "ManagerOrAdmin")]
    [HttpGet]
    public async Task<ActionResult<List<EmployeeListDto>>> GetAll()
    {
        var employees = await
            (from e in _db.Employees.AsNoTracking()
                join u in _db.Users.AsNoTracking()
                    on e.Id equals u.EmployeeId into userJoin
                from u in userJoin.DefaultIfEmpty()
                orderby e.FirstName, e.LastName
                select new EmployeeListDto
                {
                    EmployeeId = e.Id,
                    Id = u != null ? u.Id : null,
                    Role = u != null ? u.Role : null,
                    FirstName = e.FirstName,
                    LastName = e.LastName,
                    Email = e.Email,
                    Phone = e.Phone,
                    NINumber = e.NINumber,
                    HourlyRateA = e.HourlyRateA,
                    HourlyRateB = e.HourlyRateB,
                    HoursForRateA = e.HoursForRateA,
                    IsActive = e.IsActive,
                    HireDate = e.HireDate,

                    Stations = e.EmployeeStations
                        .Select(es => new EmployeeStationDto
                        {
                            StationId = es.Station.Id,
                            Code = es.Station.Code,
                            Name = es.Station.Name
                        })
                        .ToList()
                })
            .ToListAsync();

        return Ok(employees);
    }

    // ✅ Reset employee password (Admin/Manager)
    [Authorize(Policy = "ManagerOrAdmin")]
    [HttpPost("{id:guid}/reset-password")]
    public async Task<ActionResult<ResetEmployeePasswordResponseDto>> ResetPassword(Guid id)
    {
        // Find employee
        var emp = await _db.Employees.FirstOrDefaultAsync(x => x.Id == id);
        if (emp is null) return NotFound("Employee not found.");

        // Find linked user
        var user = await _db.Users.FirstOrDefaultAsync(u => u.EmployeeId == id);
        if (user is null) return NotFound("Login user not found for this employee.");

        // Generate and set new temp password
        var tempPassword = GenerateTempPassword();

        user.PasswordHash = _hasher.Hash(tempPassword);
        user.MustChangePass = true;
        user.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        return Ok(new ResetEmployeePasswordResponseDto
        {
            EmployeeId = emp.Id,
            UserId = user.Id,
            Email = user.Email,
            TemporaryPassword = tempPassword,
            MustChangePass = true
        });
    }

    private static string GenerateTempPassword()
    {
        const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnpqrstuvwxyz23456789";
        var rnd = new Random();
        var arr = new char[6];

        for (var i = 0; i < arr.Length; i++)
            arr[i] = chars[rnd.Next(chars.Length)];

        return "TT@" + new string(arr);
    }
    
    // ✅ Admin/Manager can view all staff accounts (all roles)
    [Authorize(Policy = "ManagerOrAdmin")]
    [HttpGet("users")]
    public async Task<ActionResult<List<UserWithEmployeeDto>>> GetAllUsersWithEmployee()
    {
        var data =
            await (from u in _db.Users.AsNoTracking()
                join e in _db.Employees.AsNoTracking()
                    on u.EmployeeId equals e.Id into empJoin
                from e in empJoin.DefaultIfEmpty()
                orderby u.Email
                select new UserWithEmployeeDto
                {
                    Id = u.Id,
                    Email = u.Email,
                    Role = u.Role.ToLower(),
                    IsActive = u.IsActive,
                    EmployeeId = u.EmployeeId,
                    CreatedAt = u.CreatedAt,
                    LastLoginAt = u.LastLoginAt,

                    FirstName = e != null ? e.FirstName : null,
                    LastName = e != null ? e.LastName : null,
                    Phone = e != null ? e.Phone : null,
                    NINumber = e != null ? e.NINumber : null,
                    HourlyRateA = e != null ? e.HourlyRateA : null,
                    HourlyRateB = e != null ? e.HourlyRateB : null,
                    HoursForRateA = e != null ? e.HoursForRateA : null,
                    HireDate = e != null ? e.HireDate : null
                }).ToListAsync();

        return Ok(data);
    }
    
}
    
