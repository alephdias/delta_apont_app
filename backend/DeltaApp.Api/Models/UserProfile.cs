namespace DeltaApp.Api.Models;

public class UserProfile
{
    public string UserId { get; set; } = string.Empty; // PK = sub do Supabase
    public string Email { get; set; } = string.Empty;
    public string? DisplayName { get; set; }

    /// <summary>Meta diária em minutos. 360 = estagiário (6h), 480 = analista (8h).</summary>
    public int DailyTargetMinutes { get; set; } = 360;
    public DateTime FirstSeenAt { get; set; } = DateTime.UtcNow;
}
