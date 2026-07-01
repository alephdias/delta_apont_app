using System.Security.Cryptography;
using System.Text;
using DeltaApp.Api.Dtos;
using DeltaApp.Api.Extensions;
using DeltaApp.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DeltaApp.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public class AdminController : ControllerBase
{
    private readonly SupabaseAdminService _admin;
    private readonly HashSet<string> _adminEmails;

    public AdminController(SupabaseAdminService admin, IConfiguration config)
    {
        _admin = admin;
        _adminEmails = (config["Supabase:AdminEmails"] ?? "")
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(e => e.ToLowerInvariant())
            .ToHashSet();
    }

    private bool IsAdmin()
    {
        var email = User.GetEmail()?.ToLowerInvariant();
        return email is not null && _adminEmails.Contains(email);
    }

    [HttpGet("users")]
    public async Task<ActionResult<IEnumerable<AdminUserDto>>> ListUsers()
    {
        if (!IsAdmin()) return Forbid();
        return await _admin.ListUsersAsync();
    }

    [HttpPost("users")]
    public async Task<ActionResult<CreatedUserDto>> CreateUser(CreateUserDto dto)
    {
        if (!IsAdmin()) return Forbid();
        if (!_admin.Enabled) return StatusCode(503, "Administração não configurada (defina Supabase:ServiceKey).");

        var email = dto.Email?.Trim();
        if (string.IsNullOrEmpty(email) || !email.Contains('@'))
            return BadRequest("Informe um e-mail válido.");

        var password = GeneratePassword();
        var error = await _admin.CreateUserAsync(email, password);
        if (error is not null) return Conflict(error);

        return new CreatedUserDto(email, password);
    }

    private static string GeneratePassword()
    {
        const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnpqrstuvwxyz23456789";
        var sb = new StringBuilder();
        foreach (var b in RandomNumberGenerator.GetBytes(10))
            sb.Append(chars[b % chars.Length]);
        return $"{sb}@{RandomNumberGenerator.GetInt32(10, 100)}";
    }
}
