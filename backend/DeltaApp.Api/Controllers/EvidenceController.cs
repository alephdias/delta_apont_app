using System.Text;
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
public class EvidenceController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly StorageService _storage;

    public EvidenceController(AppDbContext db, StorageService storage)
    {
        _db = db;
        _storage = storage;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<EvidenceDto>>> GetForSolicitation([FromQuery] int solicitationId)
    {
        var userId = User.GetUserId();
        var list = await _db.Evidences
            .Where(e => e.UserId == userId && e.SolicitationId == solicitationId)
            .OrderByDescending(e => e.CreatedAt)
            .ToListAsync();

        var result = new List<EvidenceDto>();
        foreach (var e in list)
        {
            var url = e.Kind == EvidenceKind.Link ? e.Value : await _storage.SignAsync(e.Value);
            result.Add(new EvidenceDto(e.Id, e.SolicitationId, e.Kind.ToString(), e.Value, e.Caption, e.CreatedAt, url));
        }
        return result;
    }

    [HttpPost]
    public async Task<ActionResult<EvidenceDto>> CreateLink(CreateEvidenceDto dto)
    {
        var userId = User.GetUserId();
        if (!await OwnsAsync(userId, dto.SolicitationId)) return NotFound("Solicitação não encontrada.");
        if (string.IsNullOrWhiteSpace(dto.Value)) return BadRequest("Valor obrigatório.");

        var ev = new Evidence
        {
            UserId = userId,
            SolicitationId = dto.SolicitationId,
            Kind = EvidenceKind.Link,
            Value = dto.Value.Trim(),
            Caption = dto.Caption
        };
        _db.Evidences.Add(ev);
        await _db.SaveChangesAsync();
        return new EvidenceDto(ev.Id, ev.SolicitationId, ev.Kind.ToString(), ev.Value, ev.Caption, ev.CreatedAt, ev.Value);
    }

    [HttpPost("upload")]
    [RequestSizeLimit(26_214_400)] // 25 MB
    public async Task<ActionResult<EvidenceDto>> Upload([FromForm] int solicitationId, [FromForm] string? caption, IFormFile file)
    {
        var userId = User.GetUserId();
        if (!_storage.Enabled)
            return StatusCode(503, "Armazenamento não configurado (defina Supabase:ServiceKey).");
        if (!await OwnsAsync(userId, solicitationId)) return NotFound("Solicitação não encontrada.");
        if (file is null || file.Length == 0) return BadRequest("Arquivo vazio.");

        var objectPath = $"{userId}/{solicitationId}/{Guid.NewGuid():N}_{Sanitize(file.FileName)}";
        await using (var stream = file.OpenReadStream())
            await _storage.UploadAsync(objectPath, file.ContentType, stream);

        var ev = new Evidence
        {
            UserId = userId,
            SolicitationId = solicitationId,
            Kind = EvidenceKind.File,
            Value = objectPath,
            Caption = string.IsNullOrWhiteSpace(caption) ? file.FileName : caption
        };
        _db.Evidences.Add(ev);
        await _db.SaveChangesAsync();

        var url = await _storage.SignAsync(objectPath);
        return new EvidenceDto(ev.Id, ev.SolicitationId, ev.Kind.ToString(), ev.Value, ev.Caption, ev.CreatedAt, url);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var userId = User.GetUserId();
        var ev = await _db.Evidences.FirstOrDefaultAsync(e => e.Id == id && e.UserId == userId);
        if (ev is null) return NotFound();
        if (ev.Kind == EvidenceKind.File) await _storage.DeleteAsync(ev.Value);
        _db.Evidences.Remove(ev);
        await _db.SaveChangesAsync();
        return NoContent();
    }

    private Task<bool> OwnsAsync(string userId, int solicitationId)
        => _db.Solicitations.AnyAsync(s => s.Id == solicitationId && s.UserId == userId);

    private static string Sanitize(string name)
    {
        name = Path.GetFileName(name);
        var sb = new StringBuilder(name.Length);
        foreach (var c in name)
            sb.Append(char.IsLetterOrDigit(c) || c is '.' or '-' or '_' ? c : '_');
        var safe = sb.ToString().Trim('_');
        if (safe.Length == 0) safe = "arquivo";
        return safe.Length > 80 ? safe[^80..] : safe;
    }
}
