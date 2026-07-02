using DeltaApp.Api.Models;

namespace DeltaApp.Api.Dtos;

// ClientId tem prioridade; se vier ClientName (digitar livre), a API acha ou cria o cliente.
public record CreateSolicitationDto(
    SolicitationType Type,
    string Number,
    int? ClientId,
    string? ClientName,
    string? Title,
    string? Description,
    string? Tags);

public record UpdateSolicitationDto(
    int? ClientId,
    string? ClientName,
    string? Title,
    string? Description,
    SolicitationStatus Status,
    string? Tags,
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
    string Status,
    string? Tags,
    bool IsArchived,
    DateTime CreatedAt);
