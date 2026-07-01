using System.Text.Json;

namespace DeltaApp.Desktop;

public class AuthSession
{
    public required string AccessToken { get; init; }
    public required string RefreshToken { get; init; }
    public required string Email { get; init; }
    public bool MustChangePassword { get; set; }
}

/// <summary>Sessão autenticada corrente (em memória).</summary>
public static class Session
{
    public static AuthSession? Current { get; set; }
}

public static class Json
{
    public static readonly JsonSerializerOptions Options = new() { PropertyNameCaseInsensitive = true };
}
