using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TandTFuel.Api.Data;
using TandTFuel.Api.DTOs.Auth;
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

        var emp = await _db.Employees.AsNoTracking().FirstOrDefaultAsync(x => x.Id == employeeId);
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

    // ✅ Get Employee by UserId (id is UserId) + ONLY active assigned stations
    [Authorize(Policy = "ManagerOrAdmin")]
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

                 // ✅ ONLY stations assigned AND active
                 Stations = e.EmployeeStations
                     .Where(es => es.IsActive && es.Station.IsActive)
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

    // ✅ Get stations for employee (ONLY assigned + active)
    [Authorize(Policy = "SupervisorOrAbove")]
    [HttpGet("{id:guid}/stations")]
    public async Task<ActionResult<List<StationDto>>> GetStationsForEmployee(Guid id)
    {
        var stations = await _db.EmployeeStations
            .Where(es => es.EmployeeId == id && es.IsActive && es.Station.IsActive)
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

            // 5) Assign stations (ACTIVE links; DO NOT delete in future)
            for (var i = 0; i < stationIds.Count; i++)
            {
                _db.EmployeeStations.Add(new EmployeeStation
                {
                    EmployeeId = employee.Id,
                    StationId = stationIds[i],
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = null
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

    // ✅ Get all employees with ONLY active assigned stations 
    
[Authorize(Policy = "ManagerOrAdmin")]
[HttpGet]
public async Task<ActionResult<List<EmployeeListDto>>> GetAll()
{
    // ✅ role claim in your token is:
    // http://schemas.microsoft.com/ws/2008/06/identity/claims/role
    var actorRole = User.FindFirstValue(ClaimTypes.Role) ?? "";
    var isAdmin = actorRole.Equals("admin", StringComparison.OrdinalIgnoreCase);

    // ✅ employeeId claim may be missing for admin
    var employeeIdClaim = User.FindFirstValue("employeeId");

    // -----------------------------
    // ADMIN: see all employees
    // -----------------------------
    if (isAdmin && string.IsNullOrWhiteSpace(employeeIdClaim))
    {
        var employeesAll = await
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

                 // ✅ if there is no linked User row, fallback
                 IsActive = u != null ? u.IsActive : e.IsActive,
                 HireDate = e.HireDate,

                 // ✅ Only active employee-station links + active stations
                 Stations = e.EmployeeStations
                     .Where(es => es.IsActive && es.Station.IsActive)
                     .Select(es => new EmployeeStationDto
                     {
                         StationId = es.Station.Id,
                         Code = es.Station.Code,
                         Name = es.Station.Name
                     })
                     .ToList()
             })
            .ToListAsync();

        return Ok(employeesAll);
    }

    // -----------------------------
    // NON-ADMIN: station scoped
    // -----------------------------
    if (string.IsNullOrWhiteSpace(employeeIdClaim))
        return Forbid("This account is not linked to an employee (no employeeId claim).");

    if (!Guid.TryParse(employeeIdClaim, out var actorEmployeeId))
        return Unauthorized("Invalid employeeId claim.");

    // ✅ Get stationIds assigned to the logged-in user (only active link + active station)
    var myStationIds = await _db.EmployeeStations
        .AsNoTracking()
        .Where(es => es.EmployeeId == actorEmployeeId && es.IsActive && es.Station.IsActive)
        .Select(es => es.StationId)
        .Distinct()
        .ToListAsync();

    if (myStationIds.Count == 0)
        return Ok(new List<EmployeeListDto>());

    var employeesScoped = await
        (from e in _db.Employees.AsNoTracking()
         join u in _db.Users.AsNoTracking()
            on e.Id equals u.EmployeeId into userJoin
         from u in userJoin.DefaultIfEmpty()

         // ✅ employee must share at least one active station with logged-in user
         where e.EmployeeStations.Any(es =>
             es.IsActive &&
             es.Station.IsActive &&
             myStationIds.Contains(es.StationId))

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

             IsActive = u != null ? u.IsActive : e.IsActive,
             HireDate = e.HireDate,

             // ✅ ONLY stations that are:
             // - active employee-station link
             // - active station
             // - inside logged-in user's station scope
             Stations = e.EmployeeStations
                 .Where(es => es.IsActive && es.Station.IsActive && myStationIds.Contains(es.StationId))
                 .Select(es => new EmployeeStationDto
                 {
                     StationId = es.Station.Id,
                     Code = es.Station.Code,
                     Name = es.Station.Name
                 })
                 .ToList()
         })
        .ToListAsync();

    return Ok(employeesScoped);
}

    // ✅ Reset employee password (Admin/Manager)
    [Authorize(Policy = "ManagerOrAdmin")]
    [HttpPost("{id:guid}/reset-password")]
    public async Task<ActionResult<ResetEmployeePasswordResponseDto>> ResetPassword(Guid id)
    {
        var emp = await _db.Employees.FirstOrDefaultAsync(x => x.Id == id);
        if (emp is null) return NotFound("Employee not found.");

        var user = await _db.Users.FirstOrDefaultAsync(u => u.EmployeeId == id);
        if (user is null) return NotFound("Login user not found for this employee.");

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

    // ✅ Admin/Manager can view all staff accounts (all roles)
    [Authorize(Policy = "ManagerOrAdmin")]
[HttpGet("users")]
public async Task<ActionResult<List<UserWithEmployeeDto>>> GetAllUsersWithEmployee()
{
    var actorRole = User.FindFirstValue(ClaimTypes.Role) ?? "";
    var isAdmin = actorRole.Equals("admin", StringComparison.OrdinalIgnoreCase);

    var employeeIdClaim = User.FindFirstValue("employeeId");

    // ✅ Admin without employeeId => see all
    if (isAdmin && string.IsNullOrWhiteSpace(employeeIdClaim))
    {
        var all = await (from u in _db.Users.AsNoTracking()
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

        return Ok(all);
    }

    // ✅ Non-admin => must be employee-linked for station scoping
    if (string.IsNullOrWhiteSpace(employeeIdClaim))
        return Forbid("Not an employee account (missing employeeId claim).");

    if (!Guid.TryParse(employeeIdClaim, out var actorEmployeeId))
        return Unauthorized("Invalid employeeId claim.");

    // ✅ stations assigned to logged-in user (active link + active station)
    var myStationIds = await _db.EmployeeStations
        .AsNoTracking()
        .Where(es => es.EmployeeId == actorEmployeeId && es.IsActive && es.Station.IsActive)
        .Select(es => es.StationId)
        .Distinct()
        .ToListAsync();

    if (myStationIds.Count == 0)
        return Ok(new List<UserWithEmployeeDto>());

    // ✅ only users whose employee has at least one station in myStationIds (active+active)
    var scoped = await (from u in _db.Users.AsNoTracking()
        join e in _db.Employees.AsNoTracking()
            on u.EmployeeId equals e.Id
        where e.EmployeeStations.Any(es =>
            es.IsActive &&
            es.Station.IsActive &&
            myStationIds.Contains(es.StationId))
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

            FirstName = e.FirstName,
            LastName = e.LastName,
            Phone = e.Phone,
            NINumber = e.NINumber,
            HourlyRateA = e.HourlyRateA,
            HourlyRateB = e.HourlyRateB,
            HoursForRateA = e.HoursForRateA,
            HireDate = e.HireDate
        }).ToListAsync();


    return Ok(scoped);
}

[Authorize(Policy = "ManagerOrAdmin")]
[HttpPut("{id:guid}")]
public async Task<IActionResult> Update(Guid id, [FromBody] EmployeeUpdateDto dto)
{
    var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == id);
    
    if (user is null) return NotFound("User not found.");
    if (user.EmployeeId is null)
        return BadRequest("This user has no employee record.");

    var emp = await _db.Employees.FirstOrDefaultAsync(e => e.Id == user.EmployeeId.Value);
    if (emp is null) return BadRequest("Employee record not found.");

    await using var tx = await _db.Database.BeginTransactionAsync();

    try
    {
        // 🔹 EMAIL VALIDATION
        var email = dto.Email.Trim().ToLower();
        var actorRole = User.FindFirst("role")?.Value ?? "";
        var isAdmin = User.IsInRole("admin");

        if (await _db.Employees.AnyAsync(e => e.Email == email && e.Id != emp.Id))
            return BadRequest("Employee email already exists.");

        if (await _db.Users.AnyAsync(u => u.Email == email && u.Id != user.Id))
            return BadRequest("User email already exists.");

        // 🔹 USER UPDATE
        user.Email = email;

        if (isAdmin)
        {
            if (!string.IsNullOrWhiteSpace(dto.Role))
                user.Role = dto.Role.Trim().ToLower();

            if (dto.IsActive.HasValue)
                user.IsActive = dto.IsActive.Value;
        }

        // 🔹 EMPLOYEE UPDATE
        emp.FirstName = dto.FirstName.Trim();
        emp.LastName = dto.LastName.Trim();
        emp.Email = email;
        emp.Phone = string.IsNullOrWhiteSpace(dto.Phone) ? null : dto.Phone.Trim();
        emp.NINumber = string.IsNullOrWhiteSpace(dto.NINumber) ? null : dto.NINumber.Trim().ToUpper();
        emp.HourlyRateA = dto.HourlyRateA;
        emp.HourlyRateB = dto.HourlyRateB;
        emp.HoursForRateA = dto.HoursForRateA;

        // 🔹 STATIONS (admin-only, soft assign)
        if (isAdmin && dto.StationIds != null)
        {
            var stationIds = dto.StationIds.Distinct().ToList();
            if (stationIds.Count == 0)
                return BadRequest("Select at least one station.");

            var activeStations = await _db.Stations
                .Where(s => stationIds.Contains(s.Id) && s.IsActive)
                .Select(s => s.Id)
                .ToListAsync();

            if (activeStations.Count != stationIds.Count)
                return BadRequest("One or more stations are invalid or inactive.");

            var links = await _db.EmployeeStations
                .Where(es => es.EmployeeId == emp.Id)
                .ToListAsync();

            // Soft-disable all
            foreach (var l in links)
                l.IsActive = false;

            // Enable selected
            foreach (var stId in activeStations)
            {
                var link = links.FirstOrDefault(x => x.StationId == stId);
                if (link != null)
                {
                    link.IsActive = true;
                    link.UpdatedAt = DateTime.UtcNow;
                }
                else
                {
                    _db.EmployeeStations.Add(new EmployeeStation
                    {
                        EmployeeId = emp.Id,
                        StationId = stId,
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow
                    });
                }
            }
        }

        await _db.SaveChangesAsync();
        await tx.CommitAsync();

        return Ok(new { message = "Employee updated successfully." });
    }
    catch
    {
        await tx.RollbackAsync();
        throw;
    }
}

// ✅ Upate and toggle employee active status Manager/Admin
    [Authorize(Policy = "ManagerOrAdmin")]
    [HttpPatch("{id:guid}/status")]
    public async Task<IActionResult> ToggleStatus(Guid id)
    {
      
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == id);
        if (user is null) return NotFound(new { message = "User not found for this employee." });
        
        //get the employee
        var emp = await _db.Employees.FirstOrDefaultAsync(e => e.Id == user.EmployeeId);
        if (emp is null) return NotFound(new { message = "Employee record not found." });
        
        emp.IsActive = !emp.IsActive;

        user.IsActive = !user.IsActive;
        user.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        return Ok(new
        {
            message = $"Employee has been {(emp.IsActive ? "activated" : "deactivated")}.",
            isActive = user.IsActive
        });
    }
    
    //Update user role (Admin-only)
    [Authorize(Policy = "AdminOnly")]
    [HttpPut("{id:guid}/role")]
    public async Task<IActionResult> UpdateUserRole(Guid id, [FromBody] UpdateUserRoleDto dto)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == id);
        if (user is null) return NotFound("User not found.");
        user.Role = dto.Role.Trim().ToLower();
        user.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return Ok(new { message = "User role updated." });
    }
    // ✅ Change password (admin-only or self)
    [Authorize]
    [HttpPut("{id:guid}/change-password")]
    public async Task<IActionResult> ChangePassword(Guid id, [FromBody] ChangePasswordDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.NewPassword) || dto.NewPassword.Length < 8)
            return BadRequest("Password must be at least 8 characters.");

        var actorUserId = User.FindFirst("userId")?.Value;
        var actorRole = User.FindFirst("role")?.Value ?? "";

        var isAdmin = actorRole == "admin";
        var isSelf = actorUserId != null && Guid.TryParse(actorUserId, out var actorGuid) && actorGuid == id;

        if (!isAdmin && !isSelf)
            return Forbid("You can only change your own password.");

        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == id);
        if (user is null) return NotFound("User not found.");

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.NewPassword);
        user.MustChangePass = false;

        await _db.SaveChangesAsync();
        return Ok(new { message = "Password updated." });
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
}
