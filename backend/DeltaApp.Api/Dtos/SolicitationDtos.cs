using DeltaApp.Api.Models;

namespace DeltaApp.Api.Dtos;

// ClientId tem prioridade; se vier ClientName (digitar livre), a API acha ou cria o cliente.
public record CreateSolicitationDto(
    SolicitationType Type,
    string Number,
    int? ClientId,
    string? ClientName,
    string? Title,
    string? Description);

public record UpdateSolicitationDto(
    int? ClientId,
    string? ClientName,
    string? Title,
    string? Description,
    bool IsArchived);

public record SolicitationDto(
    int Id,
    string Type,
    string Number,
    string Code,
    int? ClientId,
    string? ClientName,
    string? Title,
    string? Description,
    bool IsArchived,
    DateTime CreatedAt);
