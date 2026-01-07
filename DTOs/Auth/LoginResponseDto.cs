namespace TandTFuel.Api.DTOs.Auth;

public class LoginResponseDto
{
    public string Token { get; set; } = "";
    public string RefreshToken { get; set; } = ""; // optional (can be empty for now)
    public AuthUserDto User { get; set; } = null!;
}