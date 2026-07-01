using System.IO;
using System.Text.Json;

namespace DeltaApp.Desktop;

/// <summary>
/// Configuração do app. Traz valores padrão embutidos (todos públicos) para que o
/// executável de arquivo único funcione sozinho. Um appsettings.json ao lado do
/// executável, se existir, sobrescreve esses valores.
/// </summary>
public static class Config
{
    public static string SupabaseUrl { get; private set; } = "https://yfbwictslagaaqevyxvn.supabase.co";
    public static string SupabaseAnonKey { get; private set; } = "sb_publishable__YDhEtSs8PZkxx_9R2XHMw_OClUp-MR";
    public static string ApiBaseUrl { get; private set; } = "https://delta-apont-api.onrender.com/api/";

    public static void Load()
    {
        try
        {
            var path = Path.Combine(AppContext.BaseDirectory, "appsettings.json");
            if (File.Exists(path))
            {
                using var doc = JsonDocument.Parse(File.ReadAllText(path));
                var root = doc.RootElement;
                if (root.TryGetProperty("SupabaseUrl", out var u) && u.GetString() is { } us) SupabaseUrl = us;
                if (root.TryGetProperty("SupabaseAnonKey", out var k) && k.GetString() is { } ks) SupabaseAnonKey = ks;
                if (root.TryGetProperty("ApiBaseUrl", out var a) && a.GetString() is { } aps) ApiBaseUrl = aps;
            }
        }
        catch
        {
            // mantém os valores padrão embutidos
        }

        SupabaseUrl = SupabaseUrl.TrimEnd('/');
        if (!ApiBaseUrl.EndsWith('/')) ApiBaseUrl += "/";
    }
}
