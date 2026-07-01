using System.Net.Http;
using System.Text.Json;

namespace DeltaApp.Desktop.Services;

public static class UpdateService
{
    /// <summary>Verifica no GitHub se há uma versão mais nova. Retorna (temNova, tag, urlDaPagina).</summary>
    public static async Task<(bool available, string tag, string url)> CheckAsync()
    {
        try
        {
            using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(8) };
            http.DefaultRequestHeaders.UserAgent.ParseAdd("DeltaApont");
            var json = await http.GetStringAsync($"https://api.github.com/repos/{AppInfo.Repo}/releases/latest");
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            var tag = root.GetProperty("tag_name").GetString() ?? "";
            var url = root.TryGetProperty("html_url", out var h) ? h.GetString() ?? AppInfo.ReleasesUrl : AppInfo.ReleasesUrl;

            if (Version.TryParse(tag.TrimStart('v', 'V'), out var latest)
                && Version.TryParse(AppInfo.Version, out var current)
                && latest > current)
            {
                return (true, tag, url);
            }
        }
        catch { /* sem internet / rate limit: ignora */ }
        return (false, "", "");
    }
}
