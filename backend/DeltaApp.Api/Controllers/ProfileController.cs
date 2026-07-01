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
    public ProfileController(AppDbContext db) => _db = db;

    /// <summary>Retorna o perfil do usuário, criando-o (upsert) no primeiro acesso.</summary>
    [HttpGet]
    public async Task<ActionResult<ProfileDto>> Get()
    {
        var profile = await GetOrCreateAsync();
        return new ProfileDto(profile.UserId, profile.Email, profile.DisplayName, profile.DailyTargetMinutes);
    }

    [HttpPut]
    public async Task<ActionResult<ProfileDto>> Update(UpdateProfileDto dto)
    {
        var profile = await GetOrCreateAsync();
        profile.DisplayName = dto.DisplayName?.Trim();
        if (dto.DailyTargetMinutes > 0) profile.DailyTargetMinutes = dto.DailyTargetMinutes;
        await _db.SaveChangesAsync();
        return new ProfileDto(profile.UserId, profile.Email, profile.DisplayName, profile.DailyTargetMinutes);
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
