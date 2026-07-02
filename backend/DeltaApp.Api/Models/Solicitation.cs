namespace DeltaApp.Api.Models;

public enum SolicitationType
{
    SO,
    PA
}

public enum SolicitationStatus
{
    Aberta,
    EmAndamento,
    Resolvida
}

public class Solicitation
{
    public int Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public SolicitationType Type { get; set; }
    public string Number { get; set; } = string.Empty;

    public int? ClientId { get; set; }
    public Client? Client { get; set; }

    public string? Title { get; set; }
    public string? Description { get; set; }
    public SolicitationStatus Status { get; set; } = SolicitationStatus.Aberta;
    /// <summary>Etiquetas separadas por vírgula (ex.: "retrabalho,urgente").</summary>
    public string? Tags { get; set; }
    public bool IsArchived { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<WorkInterval> Intervals { get; set; } = new List<WorkInterval>();
    public ICollection<DayEntry> DayEntries { get; set; } = new List<DayEntry>();
    public ICollection<Evidence> Evidences { get; set; } = new List<Evidence>();

    /// <summary>Código exibido, ex.: "SO-26061542". Não é persistido.</summary>
    public string Code => $"{Type}-{Number}";
}
