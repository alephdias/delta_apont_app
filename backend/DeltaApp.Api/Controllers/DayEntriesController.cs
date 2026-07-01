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
public class DayEntriesController : ControllerBase
{
    private readonly AppDbContext _db;
    public DayEntriesController(AppDbContext db) => _db = db;

    /// <summary>Linhas do dia: tudo que foi trabalhado (derivado dos intervalos) + apontado/notas do DayEntry.</summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<DayEntryDto>>> GetByDate([FromQuery] DateOnly date)
    {
        var userId = User.GetUserId();
        var now = DateTime.UtcNow;
        var (startUtc, endUtc) = AppTime.LocalDayRangeUtc(date);

        var intervals = await _db.WorkIntervals
            .Include(i => i.Solicitation).ThenInclude(s => s!.Client)
            .Where(i => i.UserId == userId && i.StartedAt >= startUtc && i.StartedAt < endUtc)
            .ToListAsync();

        var entries = await _db.DayEntries
            .Where(e => e.UserId == userId && e.WorkDate == date)
            .ToListAsync();
        var entryBySol = entries.ToDictionary(e => e.SolicitationId);

        var rows = new List<DayEntryDto>();

        foreach (var g in intervals.GroupBy(i => i.SolicitationId))
        {
            var sol = g.First().Solicitation!;
            var realMin = TimeMath.RealMinutes(g, now);
            var isRunning = g.Any(i => i.EndedAt == null);
            var firstStart = AppTime.LocalTime(g.Min(i => i.StartedAt));
            var lastEnd = isRunning ? (TimeOnly?)null : AppTime.LocalTime(g.Max(i => i.EndedAt!.Value));
            var suggested = TimeMath.RoundUpTo15(realMin);
            entryBySol.TryGetValue(sol.Id, out var entry);

            rows.Add(new DayEntryDto(
                entry?.Id ?? 0, sol.Id, sol.Code, sol.Type.ToString(), sol.Client?.Name, sol.Title,
                date, realMin, entry?.AdjustedMinutes ?? suggested, suggested,
                firstStart, lastEnd, isRunning, entry?.Notes));
        }

        // DayEntries sem intervalos nesse dia (ajuste manual / lançamento avulso).
        var solIdsWithIntervals = intervals.Select(i => i.SolicitationId).ToHashSet();
        foreach (var e in entries.Where(e => !solIdsWithIntervals.Contains(e.SolicitationId)))
        {
            var sol = await _db.Solicitations.Include(s => s.Client).FirstAsync(s => s.Id == e.SolicitationId);
            rows.Add(new DayEntryDto(
                e.Id, sol.Id, sol.Code, sol.Type.ToString(), sol.Client?.Name, sol.Title,
                date, 0, e.AdjustedMinutes, 0, null, null, false, e.Notes));
        }

        return rows.OrderBy(r => r.FirstStart ?? TimeOnly.MaxValue).ToList();
    }

    /// <summary>Resumo do fechamento mensal: total apontado por dia vs meta.</summary>
    [HttpGet("month")]
    public async Task<ActionResult<MonthSummaryDto>> GetMonth([FromQuery] string month)
    {
        var userId = User.GetUserId();
        if (!TryParseMonth(month, out var year, out var mon))
            return BadRequest("Formato de mês inválido (use YYYY-MM).");

        var first = new DateOnly(year, mon, 1);
        var last = first.AddMonths(1);

        var entries = await _db.DayEntries
            .Where(e => e.UserId == userId && e.WorkDate >= first && e.WorkDate < last)
            .ToListAsync();

        var profile = await _db.UserProfiles.FindAsync(userId);
        var target = profile?.DailyTargetMinutes ?? 360;

        var days = entries
            .GroupBy(e => e.WorkDate)
            .Select(g =>
            {
                var total = g.Sum(x => x.AdjustedMinutes);
                return new MonthDaySummaryDto(g.Key, total, target, total >= target);
            })
            .OrderBy(d => d.WorkDate)
            .ToList();

        return new MonthSummaryDto(month, target, days.Sum(d => d.TotalAdjustedMinutes), days);
    }

    /// <summary>Cria/atualiza o apontado e notas de uma solicitação em um dia.</summary>
    [HttpPut]
    public async Task<ActionResult<DayEntryDto>> Upsert(UpsertDayEntryDto dto)
    {
        var userId = User.GetUserId();
        if (!TimeMath.IsMultipleOf15(dto.AdjustedMinutes))
            return BadRequest("O tempo apontado deve ser múltiplo de 15 minutos.");

        var sol = await _db.Solicitations.Include(s => s.Client)
            .FirstOrDefaultAsync(s => s.Id == dto.SolicitationId && s.UserId == userId);
        if (sol is null) return NotFound("Solicitação não encontrada.");

        var entry = await _db.DayEntries.FirstOrDefaultAsync(e =>
            e.UserId == userId && e.SolicitationId == dto.SolicitationId && e.WorkDate == dto.WorkDate);
        if (entry is null)
        {
            entry = new DayEntry { UserId = userId, SolicitationId = dto.SolicitationId, WorkDate = dto.WorkDate };
            _db.DayEntries.Add(entry);
        }
        entry.AdjustedMinutes = dto.AdjustedMinutes;
        entry.Notes = dto.Notes;
        await _db.SaveChangesAsync();

        return new DayEntryDto(entry.Id, sol.Id, sol.Code, sol.Type.ToString(), sol.Client?.Name, sol.Title,
            entry.WorkDate, 0, entry.AdjustedMinutes, 0, null, null, false, entry.Notes);
    }

    private static bool TryParseMonth(string? month, out int year, out int mon)
    {
        year = 0; mon = 0;
        if (string.IsNullOrWhiteSpace(month)) return false;
        var parts = month.Split('-');
        return parts.Length == 2
            && int.TryParse(parts[0], out year)
            && int.TryParse(parts[1], out mon)
            && mon is >= 1 and <= 12;
    }
}
