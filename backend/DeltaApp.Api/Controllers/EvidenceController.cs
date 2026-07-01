using DeltaApp.Api.Data;
using DeltaApp.Api.Dtos;
using DeltaApp.Api.Extensions;
using DeltaApp.Api.Models;
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
    public EvidenceController(AppDbContext db) => _db = db;

    [HttpGet]
    public async Task<ActionResult<IEnumerable<EvidenceDto>>> GetForSolicitation([FromQuery] int solicitationId)
    {
        var userId = User.GetUserId();
        return await _db.Evidences
            .Where(e => e.UserId == userId && e.SolicitationId == solicitationId)
            .OrderByDescending(e => e.CreatedAt)
            .Select(e => new EvidenceDto(e.Id, e.SolicitationId, e.Kind.ToString(), e.Value, e.Caption, e.CreatedAt))
            .ToListAsync();
    }

    [HttpPost]
    public async Task<ActionResult<EvidenceDto>> Create(CreateEvidenceDto dto)
    {
        var userId = User.GetUserId();
        var owns = await _db.Solicitations.AnyAsync(s => s.Id == dto.SolicitationId && s.UserId == userId);
        if (!owns) return NotFound("Solicitação não encontrada.");
        if (string.IsNullOrWhiteSpace(dto.Value)) return BadRequest("Valor obrigatório.");

        var ev = new Evidence
        {
            UserId = userId,
            SolicitationId = dto.SolicitationId,
            Kind = dto.Kind,
            Value = dto.Value.Trim(),
            Caption = dto.Caption
        };
        _db.Evidences.Add(ev);
        await _db.SaveChangesAsync();
        return CreatedAtAction(nameof(GetForSolicitation),
            new EvidenceDto(ev.Id, ev.SolicitationId, ev.Kind.ToString(), ev.Value, ev.Caption, ev.CreatedAt));
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var userId = User.GetUserId();
        var ev = await _db.Evidences.FirstOrDefaultAsync(e => e.Id == id && e.UserId == userId);
        if (ev is null) return NotFound();
        _db.Evidences.Remove(ev);
        await _db.SaveChangesAsync();
        return NoContent();
    }
}
