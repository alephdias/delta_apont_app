namespace DeltaApp.Api.Dtos;

public record DayEntryDto(
    int Id,
    int SolicitationId,
    string Code,
    string Type,
    string? ClientName,
    string? Title,
    DateOnly WorkDate,
    int RealMinutes,
    int AdjustedMinutes,
    int SuggestedMinutes,
    TimeOnly? FirstStart,
    TimeOnly? LastEnd,
    bool IsRunning,
    string? Notes);

// Upsert por (usuário, solicitação, dia).
public record UpsertDayEntryDto(
    int SolicitationId,
    DateOnly WorkDate,
    int AdjustedMinutes,
    string? Notes);

public record MonthDaySummaryDto(DateOnly WorkDate, int TotalAdjustedMinutes, int TargetMinutes, bool MetTarget);

public record MonthSummaryDto(
    string Month,
    int TargetMinutes,
    int TotalAdjustedMinutes,
    IEnumerable<MonthDaySummaryDto> Days);
