using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using DeltaApp.Desktop.Models;

namespace DeltaApp.Desktop.Services;

/// <summary>Cliente da API .NET. Renova o token do Supabase automaticamente ao levar 401.</summary>
public class ApiClient
{
    private static readonly HttpClient Http = new() { BaseAddress = new Uri(Config.ApiBaseUrl) };
    private static readonly SupabaseAuthService Auth = new();
    private static readonly SemaphoreSlim RefreshLock = new(1, 1);

    // ----- Núcleo: envia com Bearer, e em 401 renova o token e repete uma vez -----
    private static async Task<HttpResponseMessage> ExecuteAsync(Func<HttpRequestMessage> factory)
    {
        var req = factory();
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", Session.Current!.AccessToken);
        var resp = await Http.SendAsync(req);

        if (resp.StatusCode == HttpStatusCode.Unauthorized && await TryRefreshAsync())
        {
            resp.Dispose();
            var retry = factory();
            retry.Headers.Authorization = new AuthenticationHeaderValue("Bearer", Session.Current!.AccessToken);
            resp = await Http.SendAsync(retry);
        }
        return resp;
    }

    private static async Task<bool> TryRefreshAsync()
    {
        var current = Session.Current;
        if (current is null || string.IsNullOrEmpty(current.RefreshToken)) return false;

        var tokenBefore = current.AccessToken;
        await RefreshLock.WaitAsync();
        try
        {
            // Se outra chamada já renovou enquanto esperávamos o lock, aproveita.
            if (Session.Current is not null && Session.Current.AccessToken != tokenBefore)
                return true;

            var session = await Auth.RefreshAsync(current.RefreshToken);
            if (session is null) return false;

            Session.Current = session;
            SessionStore.SaveRefreshToken(session.RefreshToken);
            return true;
        }
        catch { return false; }
        finally { RefreshLock.Release(); }
    }

    private static async Task<string> EnsureSuccessAsync(HttpResponseMessage resp)
    {
        var s = await resp.Content.ReadAsStringAsync();
        if (resp.IsSuccessStatusCode) return s;
        if (resp.StatusCode == HttpStatusCode.Unauthorized)
            throw new InvalidOperationException("Sua sessão expirou. Feche e abra o app para entrar novamente.");
        throw new InvalidOperationException($"API {(int)resp.StatusCode}: {s}");
    }

    private static async Task<T?> SendAsync<T>(HttpMethod method, string path, object? body = null)
    {
        using var resp = await ExecuteAsync(() => BuildRaw(method, path, body));
        var s = await EnsureSuccessAsync(resp);
        return string.IsNullOrWhiteSpace(s) ? default : JsonSerializer.Deserialize<T>(s, Json.Options);
    }

    private static async Task SendAsync(HttpMethod method, string path, object? body = null)
    {
        using var resp = await ExecuteAsync(() => BuildRaw(method, path, body));
        await EnsureSuccessAsync(resp);
    }

    private static HttpRequestMessage BuildRaw(HttpMethod method, string path, object? body)
    {
        var req = new HttpRequestMessage(method, path);
        if (body is not null) req.Content = JsonContent.Create(body);
        return req; // Authorization é adicionado no ExecuteAsync (pra permitir retry após refresh)
    }

    // ----- Profile -----
    public Task<ProfileDto?> GetProfileAsync() => SendAsync<ProfileDto>(HttpMethod.Get, "profile");

    // ----- Clients -----
    public Task<List<ClientDto>?> GetClientsAsync() => SendAsync<List<ClientDto>>(HttpMethod.Get, "clients");

    public Task<ClientDto?> CreateClientAsync(string name) =>
        SendAsync<ClientDto>(HttpMethod.Post, "clients", new { name });

    // ----- Solicitations -----
    public Task<List<SolicitationDto>?> GetSolicitationsAsync(DateOnly? date = null)
    {
        var path = date is DateOnly d ? $"solicitations?date={d:yyyy-MM-dd}" : "solicitations";
        return SendAsync<List<SolicitationDto>>(HttpMethod.Get, path);
    }

    public Task<SolicitationDto?> CreateSolicitationAsync(string type, string number, int? clientId, string? clientName, string? title) =>
        SendAsync<SolicitationDto>(HttpMethod.Post, "solicitations", new { type, number, clientId, clientName, title });

    public Task UpdateNotesAsync(int solicitationId, string? description) =>
        SendAsync(HttpMethod.Put, $"solicitations/{solicitationId}/notes", new { description });

    // ----- Timer -----
    public Task<ActiveTimerDto?> GetActiveAsync() => SendAsync<ActiveTimerDto>(HttpMethod.Get, "timer/active");

    public Task<ActiveTimerDto?> StartAsync(int solicitationId) =>
        SendAsync<ActiveTimerDto>(HttpMethod.Post, "timer/start", new { solicitationId });

    public Task PauseAsync() => SendAsync(HttpMethod.Post, "timer/pause");

    public Task<DayEntryDto?> FinishAsync(int solicitationId) =>
        SendAsync<DayEntryDto>(HttpMethod.Post, "timer/finish", new { solicitationId });

    // ----- Day entries -----
    public Task<List<DayEntryDto>?> GetDayEntriesAsync(DateOnly date) =>
        SendAsync<List<DayEntryDto>>(HttpMethod.Get, $"dayentries?date={date:yyyy-MM-dd}");

    // ----- Evidence -----
    public Task<EvidenceDto?> AddLinkEvidenceAsync(int solicitationId, string value, string? caption) =>
        SendAsync<EvidenceDto>(HttpMethod.Post, "evidence", new { solicitationId, kind = "Link", value, caption });

    public async Task UploadEvidenceAsync(int solicitationId, byte[] data, string fileName, string contentType, string? caption = null)
    {
        using var resp = await ExecuteAsync(() =>
        {
            var form = new MultipartFormDataContent
            {
                { new StringContent(solicitationId.ToString()), "solicitationId" }
            };
            if (!string.IsNullOrEmpty(caption)) form.Add(new StringContent(caption), "caption");
            var fileContent = new ByteArrayContent(data);
            fileContent.Headers.ContentType = new MediaTypeHeaderValue(contentType);
            form.Add(fileContent, "file", fileName);
            return new HttpRequestMessage(HttpMethod.Post, "evidence/upload") { Content = form };
        });

        await EnsureSuccessAsync(resp);
    }
}
