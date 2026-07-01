namespace DeltaApp.Api.Dtos;

public record ProfileDto(string UserId, string Email, string? DisplayName, int DailyTargetMinutes, bool IsAdmin);
public record UpdateProfileDto(string? DisplayName, int DailyTargetMinutes);
