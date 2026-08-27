using System.Net.Http.Json;
using System.Text.Json;
using MindNova.Web.Models;

namespace MindNova.Web.Services;

public class ClientApiService
{
    private readonly HttpClient _http;
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public ClientApiService(HttpClient http)
    {
        _http = http;
    }

    public async Task<PagedResult<ClientModel>> GetClientsAsync(string search, int page, int pageSize)
    {
        var url = $"/api/clients?page={page}&page_size={pageSize}";
        if (!string.IsNullOrWhiteSpace(search))
            url += $"&search={Uri.EscapeDataString(search)}";

        var response = await _http.GetAsync(url);
        var content = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<PagedResult<ClientModel>>(content, JsonOptions) ?? new PagedResult<ClientModel>();
    }

    public async Task<ClientModel> GetClientAsync(Guid id)
    {
        var response = await _http.GetAsync($"/api/clients/{id}");
        var content = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<ClientModel>(content, JsonOptions);
    }

    public async Task<(ClientModel Client, string Error)> CreateClientAsync(CreateClientModel model)
    {
        var response = await _http.PostAsJsonAsync("/api/clients", model);
        var content = await response.Content.ReadAsStringAsync();

        if (content.Contains("\"Status\"") && content.Contains("\"Title\""))
            return (null, ExtractDetail(content));

        var client = JsonSerializer.Deserialize<ClientModel>(content, JsonOptions);
        return (client, null);
    }

    public async Task<(ClientModel Client, string Error)> UpdateClientAsync(Guid id, CreateClientModel model)
    {
        var response = await _http.PutAsJsonAsync($"/api/clients/{id}", model);
        var content = await response.Content.ReadAsStringAsync();

        if (content.Contains("\"Status\"") && content.Contains("\"Title\""))
            return (null, ExtractDetail(content));

        var client = JsonSerializer.Deserialize<ClientModel>(content, JsonOptions);
        return (client, null);
    }

    public async Task<List<TimelineEvent>> GetTimelineAsync(Guid clientId)
    {
        var response = await _http.GetAsync($"/api/clients/{clientId}/timeline");
        var content = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<List<TimelineEvent>>(content, JsonOptions) ?? new List<TimelineEvent>();
    }

    private static string ExtractDetail(string json)
    {
        try
        {
            var doc = JsonDocument.Parse(json);
            if (doc.RootElement.TryGetProperty("Detail", out var detail))
                return detail.GetString() ?? "An error occurred.";
            if (doc.RootElement.TryGetProperty("detail", out var detailLower))
                return detailLower.GetString() ?? "An error occurred.";
        }
        catch { }
        return "An error occurred.";
    }
}
