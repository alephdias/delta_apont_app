namespace DeltaApp.Api.Models;

public enum EvidenceKind
{
    Link,
    File
}

public class Evidence
{
    public int Id { get; set; }
    public string UserId { get; set; } = string.Empty;

    public int SolicitationId { get; set; }
    public Solicitation? Solicitation { get; set; }

    public EvidenceKind Kind { get; set; }

    /// <summary>URL/texto (Link) ou caminho no Supabase Storage (File).</summary>
    public string Value { get; set; } = string.Empty;
    public string? Caption { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
