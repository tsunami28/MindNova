namespace MindNova.Infrastructure.Auth;

public class AuthResult
{
    public bool Succeeded { get; init; }
    public string Token { get; init; } = string.Empty;
    public List<string> Errors { get; init; } = new();
}
