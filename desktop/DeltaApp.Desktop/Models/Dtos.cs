namespace DeltaApp.Desktop.Models;

public class ClientDto
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public override string ToString() => Name;
}

public class SolicitationDto
{
    public int Id { get; set; }
    public string Type { get; set; } = "SO";
    public string Number { get; set; } = "";
    public string Code { get; set; } = "";
    public int? ClientId { get; set; }
    public string? ClientName { get; set; }
    public string? Title { get; set; }
    public bool IsArchived { get; set; }
}

public class ActiveTimerDto
{
    public int IntervalId { get; set; }
    public int SolicitationId { get; set; }
    public string Code { get; set; } = "";
    public string? ClientName { get; set; }
    public DateTime StartedAt { get; set; }
    public int PriorSecondsToday { get; set; }
}

public class DayEntryDto
{
    public int Id { get; set; }
    public int SolicitationId { get; set; }
    public string Code { get; set; } = "";
    public string Type { get; set; } = "";
    public string? ClientName { get; set; }
    public string? Title { get; set; }
    public int RealMinutes { get; set; }
    public int AdjustedMinutes { get; set; }
    public int SuggestedMinutes { get; set; }
    public string? FirstStart { get; set; }
    public string? LastEnd { get; set; }
    public bool IsRunning { get; set; }
    public string? Notes { get; set; }

    // Colunas exibidas no DataGrid.
    public string RealText => FormatHelper.Minutes(RealMinutes);
    public string AdjustedText => FormatHelper.Minutes(AdjustedMinutes);
    public string StartText => Short(FirstStart);
    public string EndText => IsRunning ? "em curso" : Short(LastEnd);
    public string Number => Code.Contains('-') ? Code[(Code.IndexOf('-') + 1)..] : Code;

    private static string Short(string? t)
        => string.IsNullOrEmpty(t) ? "—" : t[..Math.Min(5, t.Length)];
}

public class EvidenceDto
{
    public int Id { get; set; }
    public int SolicitationId { get; set; }
    public string Kind { get; set; } = "";
    public string Value { get; set; } = "";
    public string? Caption { get; set; }
}

public class ProfileDto
{
    public int DailyTargetMinutes { get; set; } = 360;
}
