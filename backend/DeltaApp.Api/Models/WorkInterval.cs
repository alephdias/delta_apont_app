namespace DeltaApp.Api.Models;

/// <summary>Um intervalo bruto do cronômetro. Pausar fecha o intervalo aberto; continuar cria outro.</summary>
public class WorkInterval
{
    public int Id { get; set; }
    public string UserId { get; set; } = string.Empty;

    public int SolicitationId { get; set; }
    public Solicitation? Solicitation { get; set; }

    public DateTime StartedAt { get; set; }
    public DateTime? EndedAt { get; set; }
}
