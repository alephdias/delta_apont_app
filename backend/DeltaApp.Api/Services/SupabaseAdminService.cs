using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using DeltaApp.Api.Dtos;

namespace DeltaApp.Api.Services;

/// <summary>Provisiona usuários no Supabase Auth (GoTrue admin) usando a service key.</summary>
public class SupabaseAdminService
{
    private readonly HttpClient _http;
    private readonly string _base;
    private readonly bool _enabled;

    public SupabaseAdminService(HttpClient http, IConfiguration config)
    {
        _http = http;
        var url = config["Supabase:Url"]?.TrimEnd('/') ?? "";
        _base = $"{url}/auth/v1/admin";
        var key = config["Supabase:ServiceKey"];
        _enabled = !string.IsNullOrEmpty(url) && !string.IsNullOrEmpty(key);
        if (_enabled)
        {
            _http.DefaultRequestHeaders.TryAddWithoutValidation("apikey", key);
            _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", key);
        }
    }

    public bool Enabled => _enabled;

    /// <summary>Cria o usuário com senha temporária e flag de troca no 1º login. Retorna null em sucesso, ou a mensagem de erro.</summary>
    public async Task<string?> CreateUserAsync(string email, string password)
    {
        var resp = await _http.PostAsJsonAsync($"{_base}/users", new
        {
            email,
            password,
            email_confirm = true,
            user_metadata = new { must_change_password = true }
        });
        if (resp.IsSuccessStatusCode) return null;

        var body = await resp.Content.ReadAsStringAsync();
        try
        {
            using var doc = JsonDocument.Parse(body);
            foreach (var k in new[] { "msg", "message", "error_description", "error" })
                if (doc.RootElement.TryGetProperty(k, out var el) && el.ValueKind == JsonValueKind.String)
                    return el.GetString();
        }
        catch { /* ignora */ }
        return "Não foi possível criar o usuário.";
    }

    public async Task<List<AdminUserDto>> ListUsersAsync()
    {
        var resp = await _http.GetAsync($"{_base}/users?per_page=200");
        if (!resp.IsSuccessStatusCode) return new();
        var doc = await resp.Content.ReadFromJsonAsync<UsersResponse>();
        return doc?.users?
            .Select(u => new AdminUserDto(u.email ?? "", u.created_at, u.last_sign_in_at))
            .OrderBy(u => u.Email)
            .ToList() ?? new();
    }

    private sealed class UsersResponse
    {
        public List<GoTrueUser>? users { get; set; }
    }

    private sealed class GoTrueUser
    {
        public string? email { get; set; }
        public string? created_at { get; set; }
        public string? last_sign_in_at { get; set; }
    }
}
