using DeltaApp.Api.Models;

namespace DeltaApp.Api.Services;

public static class TimeMath
{
    /// <summary>Arredonda minutos para cima ao próximo múltiplo de 15.</summary>
    public static int RoundUpTo15(int minutes)
        => minutes <= 0 ? 0 : (int)(Math.Ceiling(minutes / 15.0) * 15);

    public static bool IsMultipleOf15(int minutes) => minutes >= 0 && minutes % 15 == 0;

    /// <summary>Soma a duração dos intervalos (usando `nowUtc` para o intervalo ainda aberto).</summary>
    public static int RealMinutes(IEnumerable<WorkInterval> intervals, DateTime nowUtc)
    {
        double totalSeconds = 0;
        foreach (var i in intervals)
        {
            var end = i.EndedAt ?? nowUtc;
            totalSeconds += (end - i.StartedAt).TotalSeconds;
        }
        return (int)Math.Round(totalSeconds / 60.0);
    }
}
