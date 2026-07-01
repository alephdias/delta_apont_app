namespace DeltaApp.Api.Dtos;

public record CreateClientDto(string Name);
public record UpdateClientDto(string Name);
public record ClientDto(int Id, string Name, DateTime CreatedAt, int SolicitationCount);
