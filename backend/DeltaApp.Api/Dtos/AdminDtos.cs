namespace DeltaApp.Api.Dtos;

public record CreateUserDto(string Email);
public record CreatedUserDto(string Email, string Password);
public record AdminUserDto(string Email, string? CreatedAt, string? LastSignInAt);
