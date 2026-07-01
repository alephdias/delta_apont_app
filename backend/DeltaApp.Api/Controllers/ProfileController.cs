using DeltaApp.Api.Data;
using DeltaApp.Api.Dtos;
using DeltaApp.Api.Extensions;
using DeltaApp.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DeltaApp.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public class ProfileController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly HashSet<string> _adminEmails;

    public ProfileController(AppDbContext db, IConfiguration config)
    {
        _db = db;
        _adminEmails = (config["Supabase:AdminEmails"] ?? "")
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(e => e.ToLowerInvariant())
            .ToHashSet();
    }

    [HttpGet]
    public async Task<ActionResult<ProfileDto>> Get() => Map(await GetOrCreateAsync());

    [HttpPut]
    public async Task<ActionResult<ProfileDto>> Update(UpdateProfileDto dto)
    {
        var profile = await GetOrCreateAsync();
        profile.DisplayName = dto.DisplayName?.Trim();
        if (dto.DailyTargetMinutes > 0) profile.DailyTargetMinutes = dto.DailyTargetMinutes;
        await _db.SaveChangesAsync();
        return Map(profile);
    }

    private ProfileDto Map(UserProfile p)
    {
        var isAdmin = !string.IsNullOrEmpty(p.Email) && _adminEmails.Contains(p.Email.ToLowerInvariant());
        return new ProfileDto(p.UserId, p.Email, p.DisplayName, p.DailyTargetMinutes, isAdmin);
    }

    private async Task<UserProfile> GetOrCreateAsync()
    {
        var userId = User.GetUserId();
        var profile = await _db.UserProfiles.FindAsync(userId);
        if (profile is null)
        {
            profile = new UserProfile { UserId = userId, Email = User.GetEmail() ?? string.Empty };
            _db.UserProfiles.Add(profile);
            await _db.SaveChangesAsync();
        }
        return profile;
    }
}
