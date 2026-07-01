namespace DeltaApp.Api.Models;

public class Client
{
    public int Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<Solicitation> Solicitations { get; set; } = new List<Solicitation>();
}
