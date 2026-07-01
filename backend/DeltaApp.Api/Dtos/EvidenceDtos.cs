using DeltaApp.Api.Models;

namespace DeltaApp.Api.Dtos;

public record CreateEvidenceDto(int SolicitationId, EvidenceKind Kind, string Value, string? Caption);
public record EvidenceDto(
    int Id,
    int SolicitationId,
    string Kind,
    string Value,
    string? Caption,
    DateTime CreatedAt,
    string? Url);
