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
public class TimerController : ControllerBase
{
    private readonly AppDbContext _db;
    public TimerController(AppDbContext db) => _db = db;

    /// <summary>Intervalo aberto do usuário (para restaurar a janela flutuante). Retorna null se nada rodando.</summary>
    [HttpGet("active")]
    public async Task<ActionResult<ActiveTimerDto?>> Active()
    {
        var userId = User.GetUserId();
        var open = await _db.WorkIntervals
            .Include(i => i.Solicitation).ThenInclude(s => s!.Client)
            .FirstOrDefaultAsync(i => i.UserId == userId && i.EndedAt == null);
        if (open is null) return Ok((ActiveTimerDto?)null);

        var prior = await PriorSecondsAsync(userId, open.SolicitationId, AppTime.TodayLocal());
        return Ok(new ActiveTimerDto(
            open.Id, open.SolicitationId, open.Solicitation!.Code,
            open.Solicitation.Client?.Name, open.StartedAt, prior));
    }

    [HttpPost("start")]
    public async Task<ActionResult<ActiveTimerDto>> Start(TimerActionDto dto)
    {
        var userId = User.GetUserId();
        var sol = await _db.Solicitations.Include(s => s.Client)
            .FirstOrDefaultAsync(s => s.Id == dto.SolicitationId && s.UserId == userId);
        if (sol is null) return NotFound("Solicitação não encontrada.");

        var now = DateTime.UtcNow;
        await CloseOpenIntervalsAsync(userId, now);

        var interval = new WorkInterval { UserId = userId, SolicitationId = sol.Id, StartedAt = now };
        _db.WorkIntervals.Add(interval);
        await _db.SaveChangesAsync();

        var prior = await PriorSecondsAsync(userId, sol.Id, AppTime.TodayLocal());
        return Ok(new ActiveTimerDto(interval.Id, sol.Id, sol.Code, sol.Client?.Name, interval.StartedAt, prior));
    }

    /// <summary>Pausa: fecha o intervalo aberto (qualquer solicitação).</summary>
    [HttpPost("pause")]
    public async Task<IActionResult> Pause()
    {
        var userId = User.GetUserId();
        await CloseOpenIntervalsAsync(userId, DateTime.UtcNow);
        await _db.SaveChangesAsync();
        return NoContent();
    }

    /// <summary>Finaliza a solicitação no dia: fecha intervalos abertos e cria/atualiza o DayEntry com o apontado sugerido.</summary>
    [HttpPost("finish")]
    public async Task<ActionResult<DayEntryDto>> Finish(TimerActionDto dto)
    {
        var userId = User.GetUserId();
        var now = DateTime.UtcNow;

        var open = await _db.WorkIntervals
            .Where(i => i.UserId == userId && i.EndedAt == null && i.SolicitationId == dto.SolicitationId)
            .ToListAsync();
        foreach (var o in open) o.EndedAt = now;
        await _db.SaveChangesAsync();

        var sol = await _db.Solicitations.Include(s => s.Client)
            .FirstOrDefaultAsync(s => s.Id == dto.SolicitationId && s.UserId == userId);
        if (sol is null) return NotFound();

        var workDate = AppTime.TodayLocal();
        var dayIntervals = await DayIntervalsAsync(userId, dto.SolicitationId, workDate);
        var realMin = TimeMath.RealMinutes(dayIntervals, now);
        var suggested = TimeMath.RoundUpTo15(realMin);

        var entry = await _db.DayEntries
            .FirstOrDefaultAsync(e => e.UserId == userId && e.SolicitationId == dto.SolicitationId && e.WorkDate == workDate);
        if (entry is null)
        {
            entry = new DayEntry { UserId = userId, SolicitationId = dto.SolicitationId, WorkDate = workDate, AdjustedMinutes = suggested };
            _db.DayEntries.Add(entry);
        }
        else if (entry.AdjustedMinutes == 0)
        {
            entry.AdjustedMinutes = suggested;
        }
        await _db.SaveChangesAsync();

        var firstStart = dayIntervals.Count > 0 ? AppTime.LocalTime(dayIntervals.Min(i => i.StartedAt)) : (TimeOnly?)null;
        var lastEnd = dayIntervals.Count > 0 ? AppTime.LocalTime(dayIntervals.Max(i => i.EndedAt ?? now)) : (TimeOnly?)null;
        return new DayEntryDto(entry.Id, sol.Id, sol.Code, sol.Type.ToString(), sol.Client?.Name, sol.Title,
            workDate, realMin, entry.AdjustedMinutes, suggested, firstStart, lastEnd, false, entry.Notes);
    }

    private async Task CloseOpenIntervalsAsync(string userId, DateTime now)
    {
        var open = await _db.WorkIntervals.Where(i => i.UserId == userId && i.EndedAt == null).ToListAsync();
        foreach (var o in open) o.EndedAt = now;
    }

    private async Task<List<WorkInterval>> DayIntervalsAsync(string userId, int solicitationId, DateOnly workDate)
    {
        var (startUtc, endUtc) = AppTime.LocalDayRangeUtc(workDate);
        return await _db.WorkIntervals
            .Where(i => i.UserId == userId && i.SolicitationId == solicitationId && i.StartedAt >= startUtc && i.StartedAt < endUtc)
            .ToListAsync();
    }

    /// <summary>Segundos já acumulados hoje nessa solicitação (só intervalos finalizados; o aberto conta ao vivo no cliente).</summary>
    private async Task<int> PriorSecondsAsync(string userId, int solicitationId, DateOnly workDate)
    {
        var intervals = await DayIntervalsAsync(userId, solicitationId, workDate);
        double seconds = 0;
        foreach (var i in intervals)
            if (i.EndedAt is DateTime end)
                seconds += (end - i.StartedAt).TotalSeconds;
        return (int)Math.Round(seconds);
    }
}
