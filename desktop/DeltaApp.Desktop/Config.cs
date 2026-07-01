using System.IO;
using System.Text.Json;

namespace DeltaApp.Desktop;

/// <summary>Configuração lida de appsettings.json (copiado para a pasta de saída).</summary>
public static class Config
{
    public static string SupabaseUrl { get; private set; } = "";
    public static string SupabaseAnonKey { get; private set; } = "";
    public static string ApiBaseUrl { get; private set; } = "";

    public static void Load()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "appsettings.json");
        using var doc = JsonDocument.Parse(File.ReadAllText(path));
        var root = doc.RootElement;
        SupabaseUrl = root.GetProperty("SupabaseUrl").GetString()!.TrimEnd('/');
        SupabaseAnonKey = root.GetProperty("SupabaseAnonKey").GetString()!;
        var api = root.GetProperty("ApiBaseUrl").GetString()!;
        ApiBaseUrl = api.EndsWith('/') ? api : api + "/";
    }
}
