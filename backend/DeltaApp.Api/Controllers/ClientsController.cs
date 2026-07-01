using DeltaApp.Api.Data;
using DeltaApp.Api.Dtos;
using DeltaApp.Api.Extensions;
using DeltaApp.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DeltaApp.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public class ClientsController : ControllerBase
{
    private readonly AppDbContext _db;
    public ClientsController(AppDbContext db) => _db = db;

    [HttpGet]
    public async Task<ActionResult<IEnumerable<ClientDto>>> GetAll()
    {
        var userId = User.GetUserId();
        return await _db.Clients
            .Where(c => c.UserId == userId)
            .OrderBy(c => c.Name)
            .Select(c => new ClientDto(c.Id, c.Name, c.CreatedAt, c.Solicitations.Count))
            .ToListAsync();
    }

    [HttpPost]
    public async Task<ActionResult<ClientDto>> Create(CreateClientDto dto)
    {
        var userId = User.GetUserId();
        var name = dto.Name?.Trim();
        if (string.IsNullOrEmpty(name)) return BadRequest("Nome obrigatório.");

        var existing = await _db.Clients.FirstOrDefaultAsync(c => c.UserId == userId && c.Name == name);
        if (existing is not null)
            return Ok(new ClientDto(existing.Id, existing.Name, existing.CreatedAt, existing.Solicitations.Count));

        var client = new Client { UserId = userId, Name = name };
        _db.Clients.Add(client);
        await _db.SaveChangesAsync();
        return CreatedAtAction(nameof(GetAll), new ClientDto(client.Id, client.Name, client.CreatedAt, 0));
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<ClientDto>> Update(int id, UpdateClientDto dto)
    {
        var userId = User.GetUserId();
        var name = dto.Name?.Trim();
        if (string.IsNullOrEmpty(name)) return BadRequest("Nome obrigatório.");

        var client = await _db.Clients.FirstOrDefaultAsync(c => c.Id == id && c.UserId == userId);
        if (client is null) return NotFound();

        var clash = await _db.Clients.AnyAsync(c => c.UserId == userId && c.Name == name && c.Id != id);
        if (clash) return Conflict("Já existe uma empresa com esse nome.");

        client.Name = name;
        await _db.SaveChangesAsync();
        var count = await _db.Solicitations.CountAsync(s => s.ClientId == id);
        return new ClientDto(client.Id, client.Name, client.CreatedAt, count);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var userId = User.GetUserId();
        var client = await _db.Clients.FirstOrDefaultAsync(c => c.Id == id && c.UserId == userId);
        if (client is null) return NotFound();

        var inUse = await _db.Solicitations.AnyAsync(s => s.ClientId == id);
        if (inUse)
            return Conflict("Esta empresa tem solicitações vinculadas e não pode ser excluída.");

        _db.Clients.Remove(client);
        await _db.SaveChangesAsync();
        return NoContent();
    }
}
