using DeltaApp.Api.Data;
using DeltaApp.Api.Dtos;
using DeltaApp.Api.Extensions;
using DeltaApp.Api.Models;
using DeltaApp.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DeltaApp.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public class SolicitationsController : ControllerBase
{
    private readonly AppDbContext _db;
    public SolicitationsController(AppDbContext db) => _db = db;

    [HttpGet]
    public async Task<ActionResult<IEnumerable<SolicitationDto>>> GetAll(
        int? clientId, SolicitationType? type, SolicitationStatus? status, string? tag,
        DateOnly? date, string? q, bool includeArchived = false)
    {
        var userId = User.GetUserId();
        var query = _db.Solicitations.Include(s => s.Client).Where(s => s.UserId == userId);

        if (!includeArchived) query = query.Where(s => !s.IsArchived);
        if (clientId is not null) query = query.Where(s => s.ClientId == clientId);
        if (type is not null) query = query.Where(s => s.Type == type);
        if (status is not null) query = query.Where(s => s.Status == status);
        if (!string.IsNullOrWhiteSpace(tag)) query = query.Where(s => s.Tags != null && s.Tags.Contains(tag));
        if (!string.IsNullOrWhiteSpace(q))
            query = query.Where(s => s.Number.Contains(q) || (s.Title != null && s.Title.Contains(q)));
        if (date is not null)
        {
            var (startUtc, endUtc) = AppTime.LocalDayRangeUtc(date.Value);
            query = query.Where(s => s.Intervals.Any(i => i.StartedAt >= startUtc && i.StartedAt < endUtc));
        }

        var list = await query.OrderByDescending(s => s.CreatedAt).ToListAsync();
        return list.Select(Map).ToList();
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<SolicitationDto>> GetById(int id)
    {
        var userId = User.GetUserId();
        var s = await _db.Solicitations.Include(x => x.Client)
            .FirstOrDefaultAsync(x => x.Id == id && x.UserId == userId);
        return s is null ? NotFound() : Map(s);
    }

    [HttpPost]
    public async Task<ActionResult<SolicitationDto>> Create(CreateSolicitationDto dto)
    {
        var userId = User.GetUserId();
        var number = NormalizeNumber(dto.Type, dto.Number);
        if (string.IsNullOrEmpty(number)) return BadRequest("Número obrigatório.");

        var existing = await _db.Solicitations.Include(s => s.Client)
            .FirstOrDefaultAsync(s => s.UserId == userId && s.Type == dto.Type && s.Number == number);
        if (existing is not null) return Ok(Map(existing)); // reusa a mesma SO/PA

        var clientId = await ResolveClientIdAsync(userId, dto.ClientId, dto.ClientName);
        var s = new Solicitation
        {
            UserId = userId,
            Type = dto.Type,
            Number = number,
            ClientId = clientId,
            Title = dto.Title?.Trim(),
            Description = dto.Description,
            Tags = NormalizeTags(dto.Tags)
        };
        _db.Solicitations.Add(s);
        await _db.SaveChangesAsync();
        await _db.Entry(s).Reference(x => x.Client).LoadAsync();
        return CreatedAtAction(nameof(GetById), new { id = s.Id }, Map(s));
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<SolicitationDto>> Update(int id, UpdateSolicitationDto dto)
    {
        var userId = User.GetUserId();
        var s = await _db.Solicitations.Include(x => x.Client)
            .FirstOrDefaultAsync(x => x.Id == id && x.UserId == userId);
        if (s is null) return NotFound();

        s.ClientId = await ResolveClientIdAsync(userId, dto.ClientId, dto.ClientName);
        s.Title = dto.Title?.Trim();
        s.Description = dto.Description;
        s.Status = dto.Status;
        s.Tags = NormalizeTags(dto.Tags);
        s.IsArchived = dto.IsArchived;
        await _db.SaveChangesAsync();
        await _db.Entry(s).Reference(x => x.Client).LoadAsync();
        return Map(s);
    }

    [HttpPut("{id:int}/notes")]
    public async Task<IActionResult> UpdateNotes(int id, UpdateNotesDto dto)
    {
        var userId = User.GetUserId();
        var s = await _db.Solicitations.FirstOrDefaultAsync(x => x.Id == id && x.UserId == userId);
        if (s is null) return NotFound();
        s.Description = dto.Description;
        await _db.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var userId = User.GetUserId();
        var s = await _db.Solicitations.FirstOrDefaultAsync(x => x.Id == id && x.UserId == userId);
        if (s is null) return NotFound();

        var hasTime = await _db.WorkIntervals.AnyAsync(i => i.SolicitationId == id)
                      || await _db.DayEntries.AnyAsync(d => d.SolicitationId == id);
        if (hasTime)
            return Conflict("Esta solicitação tem tempo lançado. Arquive-a em vez de excluir.");

        _db.Solicitations.Remove(s);
        await _db.SaveChangesAsync();
        return NoContent();
    }

    private async Task<int?> ResolveClientIdAsync(string userId, int? clientId, string? clientName)
    {
        if (clientId is int id)
        {
            var exists = await _db.Clients.AnyAsync(c => c.Id == id && c.UserId == userId);
            return exists ? id : null;
        }

        var name = clientName?.Trim();
        if (string.IsNullOrEmpty(name)) return null;

        var client = await _db.Clients.FirstOrDefaultAsync(c => c.UserId == userId && c.Name == name);
        if (client is null)
        {
            client = new Client { UserId = userId, Name = name };
            _db.Clients.Add(client);
            await _db.SaveChangesAsync();
        }
        return client.Id;
    }

    private static string? NormalizeTags(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return null;
        var tags = raw
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(t => t.ToLowerInvariant())
            .Distinct()
            .Take(10)
            .ToList();
        return tags.Count == 0 ? null : string.Join(",", tags);
    }

    private static string NormalizeNumber(SolicitationType type, string? raw)
    {
        var number = (raw ?? string.Empty).Trim();
        var prefix = $"{type}-";
        if (number.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            number = number[prefix.Length..].Trim();
        return number;
    }

    private static SolicitationDto Map(Solicitation s) => new(
        s.Id, s.Type.ToString(), s.Number, s.Code, s.ClientId, s.Client?.Name,
        s.Title, s.Description, s.Status.ToString(), s.Tags, s.IsArchived, s.CreatedAt);
}
