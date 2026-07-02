using System.Diagnostics;
using System.IO;
using System.Net.Http;

namespace DeltaApp.Desktop.Services;

/// <summary>
/// Atualização automática para o executável de arquivo único:
/// baixa a nova versão, e um script .cmd espera o app fechar, substitui o
/// arquivo em uso e reabre. Não precisa reinstalar.
/// </summary>
public static class Updater
{
    private static string DownloadUrl =>
        $"https://github.com/{AppInfo.Repo}/releases/latest/download/DeltaDecisao-Desktop.exe";

    /// <summary>Baixa a nova versão para um arquivo temporário. Retorna o caminho.</summary>
    public static async Task<string> DownloadAsync(IProgress<double>? progress)
    {
        var tempNew = Path.Combine(Path.GetTempPath(), "DeltaDecisao-Desktop.new.exe");

        using var http = new HttpClient { Timeout = TimeSpan.FromMinutes(10) };
        http.DefaultRequestHeaders.UserAgent.ParseAdd("DeltaApont");
        using var resp = await http.GetAsync(DownloadUrl, HttpCompletionOption.ResponseHeadersRead);
        resp.EnsureSuccessStatusCode();

        var total = resp.Content.Headers.ContentLength ?? -1;
        await using var src = await resp.Content.ReadAsStreamAsync();
        await using var dst = File.Create(tempNew);

        var buffer = new byte[81920];
        long read = 0;
        int n;
        while ((n = await src.ReadAsync(buffer)) > 0)
        {
            await dst.WriteAsync(buffer.AsMemory(0, n));
            read += n;
            if (total > 0) progress?.Report((double)read / total);
        }
        return tempNew;
    }

    /// <summary>Agenda a troca do executável (após o app fechar) e o reinício.</summary>
    public static void ApplyAndRestart(string newExePath)
    {
        var currentExe = Environment.ProcessPath
            ?? Process.GetCurrentProcess().MainModule?.FileName
            ?? throw new InvalidOperationException("Não foi possível localizar o executável atual.");
        var pid = Environment.ProcessId;
        var script = Path.Combine(Path.GetTempPath(), "delta-update.cmd");

        var cmd =
$@"@echo off
:wait
tasklist /FI ""PID eq {pid}"" 2>NUL | find ""{pid}"" >NUL
if not errorlevel 1 (
  timeout /t 1 /nobreak >NUL
  goto wait
)
:copy
copy /Y ""{newExePath}"" ""{currentExe}"" >NUL
if errorlevel 1 (
  timeout /t 1 /nobreak >NUL
  goto copy
)
start """" ""{currentExe}""
del ""{newExePath}"" >NUL 2>&1
del ""%~f0"" >NUL 2>&1
";
        File.WriteAllText(script, cmd);

        Process.Start(new ProcessStartInfo("cmd.exe", $"/c \"{script}\"")
        {
            UseShellExecute = true,
            WindowStyle = ProcessWindowStyle.Hidden,
            CreateNoWindow = true
        });
    }
}
