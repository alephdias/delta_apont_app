using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace DeltaApp.Desktop;

/// <summary>Guarda o refresh token protegido por DPAPI (por usuário do Windows).</summary>
public static class SessionStore
{
    private static string FilePath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "DeltaApont", "session.dat");

    public static void SaveRefreshToken(string token)
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);
            var data = ProtectedData.Protect(Encoding.UTF8.GetBytes(token), null, DataProtectionScope.CurrentUser);
            File.WriteAllBytes(FilePath, data);
        }
        catch { /* melhor esforço */ }
    }

    public static string? LoadRefreshToken()
    {
        try
        {
            if (!File.Exists(FilePath)) return null;
            var data = ProtectedData.Unprotect(File.ReadAllBytes(FilePath), null, DataProtectionScope.CurrentUser);
            return Encoding.UTF8.GetString(data);
        }
        catch { return null; }
    }

    public static void Clear()
    {
        try { if (File.Exists(FilePath)) File.Delete(FilePath); }
        catch { /* melhor esforço */ }
    }
}
