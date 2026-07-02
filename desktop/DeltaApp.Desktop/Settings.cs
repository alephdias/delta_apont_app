using System.IO;
using System.Text.Json;

namespace DeltaApp.Desktop;

/// <summary>Preferências locais do app (por usuário do Windows).</summary>
public static class Settings
{
    private static string FilePath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "DeltaApont", "settings.json");

    /// <summary>Se false, o modo "não perturbe" está ativo (sem auto-pausa por inatividade).</summary>
    public static bool AutoPauseWhenIdle { get; set; } = true;

    public static void Load()
    {
        try
        {
            if (!File.Exists(FilePath)) return;
            using var doc = JsonDocument.Parse(File.ReadAllText(FilePath));
            if (doc.RootElement.TryGetProperty("AutoPauseWhenIdle", out var v))
                AutoPauseWhenIdle = v.GetBoolean();
        }
        catch { /* usa padrão */ }
    }

    public static void Save()
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);
            File.WriteAllText(FilePath, JsonSerializer.Serialize(new { AutoPauseWhenIdle }));
        }
        catch { /* melhor esforço */ }
    }
}
