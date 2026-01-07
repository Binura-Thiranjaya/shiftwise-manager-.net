using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TandTFuel.Api.Data;
using TandTFuel.Api.DTOs.Auth;
using TandTFuel.Api.Services.Auth;
using TandTFuel.Api.Services.Passwords;

namespace TandTFuel.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IPasswordHasher _hasher;
    private readonly IJwtTokenService _jwt;

    public AuthController(AppDbContext db, IPasswordHasher hasher, IJwtTokenService jwt)
    {
        _db = db;
        _hasher = hasher;
        _jwt = jwt;
    }

    [HttpPost("login")]
    public async Task<ActionResult<LoginResponseDto>> Login([FromBody] LoginRequestDto dto)
    {
        var email = dto.Email.Trim().ToLower();

        var user = await _db.Users.FirstOrDefaultAsync(x => x.Email.ToLower() == email);
        if (user is null) return Unauthorized("Invalid email or password.");
        if (!user.IsActive) return Unauthorized("User is inactive.");

        if (!_hasher.Verify(dto.Password, user.PasswordHash))
            return Unauthorized("Invalid email or password.");
        var inputPassword = dto.Password.Trim();
        var storedHash = user.PasswordHash.Trim();

        if (!_hasher.Verify(inputPassword, storedHash))
            return Unauthorized("Invalid email or password.");

        user.LastLoginAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        var (token, exp) = _jwt.CreateToken(user);
        var refreshToken = Guid.NewGuid().ToString();

        return Ok(new LoginResponseDto
        {
            Token = token,
            RefreshToken = refreshToken, // later store in DB
            User = new AuthUserDto
            {
                Id = user.Id,
                Email = user.Email,
                Role = user.Role.ToLower(),
                EmployeeId = user.EmployeeId,
                MustChangePass = user.MustChangePass
            }
        });
    }

    [Authorize]
    [HttpPost("change-password")]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDto dto)
    {
        var userId = User.Claims.First(c => c.Type.EndsWith("/nameidentifier") || c.Type == "sub").Value;
        if (!Guid.TryParse(userId, out var id)) return Unauthorized();

        var user = await _db.Users.FirstOrDefaultAsync(x => x.Id == id);
        if (user is null) return Unauthorized();

        if (!_hasher.Verify(dto.CurrentPassword, user.PasswordHash))
            return BadRequest("Current password is incorrect.");

        user.PasswordHash = _hasher.Hash(dto.NewPassword);
        user.MustChangePass = false;
        user.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return Ok("Password changed.");
    }
}
