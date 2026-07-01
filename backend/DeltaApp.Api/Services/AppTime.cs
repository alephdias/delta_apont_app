namespace DeltaApp.Api.Services;

/// <summary>
/// Fuso de referência do app (America/Sao_Paulo). Timestamps são guardados em UTC;
/// o "dia de trabalho" (WorkDate) e os horários de início/fim são no horário local.
/// </summary>
public static class AppTime
{
    private static readonly TimeZoneInfo Tz = Resolve();

    private static TimeZoneInfo Resolve()
    {
        foreach (var id in new[] { "America/Sao_Paulo", "E. South America Standard Time" })
        {
            try { return TimeZoneInfo.FindSystemTimeZoneById(id); }
            catch { /* tenta o próximo */ }
        }
        return TimeZoneInfo.Utc;
    }

    public static DateTime ToLocal(DateTime utc)
        => TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(utc, DateTimeKind.Utc), Tz);

    public static DateTime ToUtc(DateTime local)
        => TimeZoneInfo.ConvertTimeToUtc(DateTime.SpecifyKind(local, DateTimeKind.Unspecified), Tz);

    public static DateOnly LocalDate(DateTime utc) => DateOnly.FromDateTime(ToLocal(utc));
    public static TimeOnly LocalTime(DateTime utc) => TimeOnly.FromDateTime(ToLocal(utc));

    /// <summary>Intervalo UTC [início, fim) que cobre um dia local.</summary>
    public static (DateTime startUtc, DateTime endUtc) LocalDayRangeUtc(DateOnly localDate)
    {
        var startLocal = localDate.ToDateTime(TimeOnly.MinValue);
        var startUtc = ToUtc(startLocal);
        return (startUtc, startUtc.AddDays(1));
    }

    public static DateOnly TodayLocal() => LocalDate(DateTime.UtcNow);
}
