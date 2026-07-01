using System.Security.Claims;

namespace DeltaApp.Api.Extensions;

public static class ClaimsPrincipalExtensions
{
    /// <summary>UUID do usuário Supabase (claim `sub`).</summary>
    public static string GetUserId(this ClaimsPrincipal user)
        => user.FindFirstValue(ClaimTypes.NameIdentifier)
           ?? user.FindFirstValue("sub")
           ?? throw new InvalidOperationException("Token sem claim de usuário (sub).");

    public static string? GetEmail(this ClaimsPrincipal user)
        => user.FindFirstValue(ClaimTypes.Email) ?? user.FindFirstValue("email");
}
