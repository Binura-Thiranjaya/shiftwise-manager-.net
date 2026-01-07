namespace TandTFuel.Api.Services.Auth;

public class JwtOptions
{
    public string Issuer { get; set; } = "";
    public string Audience { get; set; } = "";
    public string Key { get; set; } = "";
    public int ExpMinutes { get; set; } = 120;
}