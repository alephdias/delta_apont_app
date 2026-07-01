using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using DeltaApp.Desktop.Models;

namespace DeltaApp.Desktop.Services;

/// <summary>Cliente da API .NET, com Bearer do Supabase em cada request.</summary>
public class ApiClient
{
    private static readonly HttpClient Http = new() { BaseAddress = new Uri(Config.ApiBaseUrl) };

    private static HttpRequestMessage Build(HttpMethod method, string path, object? body = null)
    {
        var req = new HttpRequestMessage(method, path);
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", Session.Current!.AccessToken);
        if (body is not null) req.Content = JsonContent.Create(body);
        return req;
    }

    private static async Task<T?> SendAsync<T>(HttpRequestMessage req)
    {
        using var resp = await Http.SendAsync(req);
        var body = await resp.Content.ReadAsStringAsync();
        if (!resp.IsSuccessStatusCode)
            throw new InvalidOperationException($"API {(int)resp.StatusCode}: {body}");
        return string.IsNullOrWhiteSpace(body) ? default : JsonSerializer.Deserialize<T>(body, Json.Options);
    }

    private static async Task SendAsync(HttpRequestMessage req)
    {
        using var resp = await Http.SendAsync(req);
        if (!resp.IsSuccessStatusCode)
            throw new InvalidOperationException($"API {(int)resp.StatusCode}: {await resp.Content.ReadAsStringAsync()}");
    }

    // ----- Clients -----
    public Task<List<ClientDto>?> GetClientsAsync()
        => SendAsync<List<ClientDto>>(Build(HttpMethod.Get, "clients"));

    public Task<ClientDto?> CreateClientAsync(string name)
        => SendAsync<ClientDto>(Build(HttpMethod.Post, "clients", new { name }));

    // ----- Solicitations -----
    public Task<List<SolicitationDto>?> GetSolicitationsAsync(int? clientId = null)
    {
        var path = clientId is int id ? $"solicitations?clientId={id}" : "solicitations";
        return SendAsync<List<SolicitationDto>>(Build(HttpMethod.Get, path));
    }

    public Task<SolicitationDto?> CreateSolicitationAsync(string type, string number, int? clientId, string? clientName, string? title)
        => SendAsync<SolicitationDto>(Build(HttpMethod.Post, "solicitations",
            new { type, number, clientId, clientName, title }));

    // ----- Timer -----
    public Task<ActiveTimerDto?> GetActiveAsync()
        => SendAsync<ActiveTimerDto>(Build(HttpMethod.Get, "timer/active"));

    public Task<ActiveTimerDto?> StartAsync(int solicitationId)
        => SendAsync<ActiveTimerDto>(Build(HttpMethod.Post, "timer/start", new { solicitationId }));

    public Task PauseAsync()
        => SendAsync(Build(HttpMethod.Post, "timer/pause"));

    public Task<DayEntryDto?> FinishAsync(int solicitationId)
        => SendAsync<DayEntryDto>(Build(HttpMethod.Post, "timer/finish", new { solicitationId }));

    // ----- Day entries -----
    public Task<List<DayEntryDto>?> GetDayEntriesAsync(DateOnly date)
        => SendAsync<List<DayEntryDto>>(Build(HttpMethod.Get, $"dayentries?date={date:yyyy-MM-dd}"));

    // ----- Evidence -----
    public Task<EvidenceDto?> AddLinkEvidenceAsync(int solicitationId, string value, string? caption)
        => SendAsync<EvidenceDto>(Build(HttpMethod.Post, "evidence",
            new { solicitationId, kind = "Link", value, caption }));
}
