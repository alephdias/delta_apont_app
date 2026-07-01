using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace DeltaApp.Desktop.Services;

/// <summary>Autentica direto no GoTrue (Supabase Auth) via REST.</summary>
public class SupabaseAuthService
{
    private static readonly HttpClient Http = new();

    public async Task<AuthSession> SignInAsync(string email, string password)
    {
        var url = $"{Config.SupabaseUrl}/auth/v1/token?grant_type=password";
        using var req = new HttpRequestMessage(HttpMethod.Post, url);
        req.Headers.TryAddWithoutValidation("apikey", Config.SupabaseAnonKey);
        req.Content = JsonContent.Create(new { email, password });

        using var resp = await Http.SendAsync(req);
        var body = await resp.Content.ReadAsStringAsync();
        if (!resp.IsSuccessStatusCode)
            throw new InvalidOperationException(ExtractError(body, "E-mail ou senha inválidos."));

        var token = JsonSerializer.Deserialize<TokenResponse>(body, Json.Options)
            ?? throw new InvalidOperationException("Resposta de login inválida.");

        return new AuthSession
        {
            AccessToken = token.access_token ?? "",
            RefreshToken = token.refresh_token ?? "",
            Email = token.user?.email ?? email,
            MustChangePassword = token.user?.user_metadata?.must_change_password ?? false
        };
    }

    public async Task UpdatePasswordAsync(string accessToken, string newPassword)
    {
        var url = $"{Config.SupabaseUrl}/auth/v1/user";
        using var req = new HttpRequestMessage(HttpMethod.Put, url);
        req.Headers.TryAddWithoutValidation("apikey", Config.SupabaseAnonKey);
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        req.Content = JsonContent.Create(new { password = newPassword, data = new { must_change_password = false } });

        using var resp = await Http.SendAsync(req);
        if (!resp.IsSuccessStatusCode)
        {
            var body = await resp.Content.ReadAsStringAsync();
            throw new InvalidOperationException(ExtractError(body, "Não foi possível atualizar a senha."));
        }
    }

    private static string ExtractError(string body, string fallback)
    {
        try
        {
            using var doc = JsonDocument.Parse(body);
            foreach (var key in new[] { "msg", "error_description", "error", "message" })
                if (doc.RootElement.TryGetProperty(key, out var el) && el.ValueKind == JsonValueKind.String)
                    return el.GetString()!;
        }
        catch { /* ignora */ }
        return fallback;
    }

    private class TokenResponse
    {
        public string? access_token { get; set; }
        public string? refresh_token { get; set; }
        public GoTrueUser? user { get; set; }
    }

    private class GoTrueUser
    {
        public string? email { get; set; }
        public UserMeta? user_metadata { get; set; }
    }

    private class UserMeta
    {
        public bool must_change_password { get; set; }
    }
}
