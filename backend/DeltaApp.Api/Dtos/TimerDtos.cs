namespace DeltaApp.Api.Dtos;

public record TimerActionDto(int SolicitationId);

/// <summary>
/// Estado do cronômetro em execução. O cliente calcula o tempo ao vivo como
/// PriorSecondsToday + (agora - StartedAt), sem precisar consultar o servidor a cada segundo.
/// </summary>
public record ActiveTimerDto(
    int IntervalId,
    int SolicitationId,
    string Code,
    string? ClientName,
    DateTime StartedAt,
    int PriorSecondsToday);
