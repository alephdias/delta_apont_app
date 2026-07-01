using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace DeltaApp.Api.Services;

/// <summary>
/// Acesso ao Supabase Storage usando a service key (server-side, ignora RLS).
/// Arquivos ficam privados; o cliente recebe URLs assinadas de curta duração.
/// </summary>
public class StorageService
{
    private readonly HttpClient _http;
    private readonly string _base;
    private readonly string _bucket;
    private readonly bool _enabled;

    public StorageService(HttpClient http, IConfiguration config)
    {
        _http = http;
        var url = config["Supabase:Url"]?.TrimEnd('/') ?? "";
        _base = $"{url}/storage/v1";
        _bucket = config["Supabase:StorageBucket"] ?? "evidencias";
        var key = config["Supabase:ServiceKey"];
        _enabled = !string.IsNullOrEmpty(url) && !string.IsNullOrEmpty(key);
        if (_enabled)
        {
            _http.DefaultRequestHeaders.TryAddWithoutValidation("apikey", key);
            _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", key);
        }
    }

    public bool Enabled => _enabled;

    public async Task UploadAsync(string objectPath, string? contentType, Stream content)
    {
        using var sc = new StreamContent(content);
        sc.Headers.ContentType = new MediaTypeHeaderValue(
            string.IsNullOrEmpty(contentType) ? "application/octet-stream" : contentType);
        var resp = await _http.PostAsync($"{_base}/object/{_bucket}/{objectPath}", sc);
        resp.EnsureSuccessStatusCode();
    }

    public async Task<string?> SignAsync(string objectPath, int expiresInSeconds = 3600)
    {
        var resp = await _http.PostAsJsonAsync(
            $"{_base}/object/sign/{_bucket}/{objectPath}", new { expiresIn = expiresInSeconds });
        if (!resp.IsSuccessStatusCode) return null;
        var doc = await resp.Content.ReadFromJsonAsync<SignResponse>();
        return string.IsNullOrEmpty(doc?.signedURL) ? null : $"{_base}{doc!.signedURL}";
    }

    public async Task DeleteAsync(string objectPath)
    {
        try { await _http.DeleteAsync($"{_base}/object/{_bucket}/{objectPath}"); }
        catch { /* melhor esforço */ }
    }

    private sealed class SignResponse
    {
        public string? signedURL { get; set; }
    }
}
