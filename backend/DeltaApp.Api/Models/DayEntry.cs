namespace DeltaApp.Api.Models;

/// <summary>
/// Unidade de fechamento: o tempo apontado de uma solicitação em um dia.
/// RealMinutes/FirstStart/LastEnd são derivados dos WorkIntervals (não persistidos aqui).
/// </summary>
public class DayEntry
{
    public int Id { get; set; }
    public string UserId { get; set; } = string.Empty;

    public int SolicitationId { get; set; }
    public Solicitation? Solicitation { get; set; }

    public DateOnly WorkDate { get; set; }
    public int AdjustedMinutes { get; set; }
    public string? Notes { get; set; }
}
