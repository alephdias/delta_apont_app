namespace DeltaApp.Desktop;

public static class FormatHelper
{
    public static string Minutes(int min)
    {
        var h = min / 60;
        var m = min % 60;
        if (h == 0) return $"{m}min";
        if (m == 0) return $"{h}h";
        return $"{h}h{m:D2}";
    }

    public static string Hms(double totalSeconds)
    {
        if (totalSeconds < 0) totalSeconds = 0;
        var ts = TimeSpan.FromSeconds(totalSeconds);
        return $"{(int)ts.TotalHours:D2}:{ts.Minutes:D2}:{ts.Seconds:D2}";
    }
}
