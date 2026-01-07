namespace TandTFuel.Api.Services.Passwords;

public interface IPasswordHasher
{
    string Hash(string password);
    bool Verify(string password, string hash);
}