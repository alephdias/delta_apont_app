namespace DeltaApp.Api.Dtos;

public record TimerActionDto(int SolicitationId);

public record ActiveTimerDto(
    int IntervalId,
    int SolicitationId,
    string Code,
    string? ClientName,
    DateTime StartedAt,
    int AccumulatedTodayMinutes);
