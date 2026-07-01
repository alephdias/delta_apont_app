using DeltaApp.Desktop.Models;

namespace DeltaApp.Desktop.Services;

/// <summary>
/// Cronômetro único, compartilhado pela janela principal e pela janelinha flutuante.
/// A contagem é calculada localmente (início + tempo já acumulado); o servidor só é
/// chamado nas ações. Assim as duas janelas ficam sempre em sincronia e sem "voltar".
/// </summary>
public class TimerHub
{
    private readonly ApiClient _api;
    private double _frozenSeconds;

    public TimerHub(ApiClient api) => _api = api;

    public ActiveTimerDto? Active { get; private set; }
    public int LastSolicitationId { get; private set; }
    public string LastCode { get; private set; } = "";
    public string? LastClientName { get; private set; }

    public bool IsRunning => Active is not null;
    public bool HasFrozen => Active is null && LastSolicitationId > 0;

    /// <summary>Disparado sempre que o estado muda (iniciar/pausar/finalizar/refresh).</summary>
    public event Action? Changed;
    private void Notify() => Changed?.Invoke();

    public double ElapsedSeconds()
    {
        if (Active is null) return _frozenSeconds;
        var s = Active.StartedAt;
        var startUtc = s.Kind switch
        {
            DateTimeKind.Utc => s,
            DateTimeKind.Local => s.ToUniversalTime(),
            _ => DateTime.SpecifyKind(s, DateTimeKind.Utc)
        };
        var elapsed = Active.PriorSecondsToday + (DateTime.UtcNow - startUtc).TotalSeconds;
        return elapsed < 0 ? 0 : elapsed;
    }

    public async Task RefreshAsync()
    {
        Active = await _api.GetActiveAsync();
        if (Active is not null) Remember(Active);
        Notify();
    }

    public async Task StartAsync(int solicitationId)
    {
        var a = await _api.StartAsync(solicitationId);
        Active = a;
        if (a is not null) { Remember(a); _frozenSeconds = 0; }
        Notify();
    }

    public async Task PauseAsync()
    {
        // Otimista: congela e para imediatamente nas duas janelas; depois confirma no servidor.
        _frozenSeconds = ElapsedSeconds();
        Active = null;
        Notify();
        try { await _api.PauseAsync(); }
        catch { await RefreshAsync(); }
    }

    public async Task<DayEntryDto?> FinishAsync(int solicitationId)
    {
        Active = null;
        _frozenSeconds = 0;
        Notify();
        try { return await _api.FinishAsync(solicitationId); }
        catch { await RefreshAsync(); return null; }
    }

    private void Remember(ActiveTimerDto a)
    {
        LastSolicitationId = a.SolicitationId;
        LastCode = a.Code;
        LastClientName = a.ClientName;
    }
}
