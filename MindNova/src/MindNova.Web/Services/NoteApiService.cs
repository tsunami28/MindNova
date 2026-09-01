using System.Net.Http.Json;
using System.Text.Json;
using MindNova.Web.Models;

using static MindNova.Web.Services.ProblemDetailsDetection;

namespace MindNova.Web.Services;

public class NoteApiService
{
    private readonly HttpClient _http;
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public NoteApiService(HttpClient http)
    {
        _http = http;
    }

    public async Task<List<NoteModel>> GetSessionNotesAsync(Guid sessionId, bool includeDeleted = false)
    {
        var url = $"/api/sessions/{sessionId}/notes";
        if (includeDeleted)
            url += "?include_deleted=true";
        var response = await _http.GetAsync(url);
        var content = await response.Content.ReadAsStringAsync();

        if (IsProblemDetails(content))
            return null;

        return JsonSerializer.Deserialize<List<NoteModel>>(content, JsonOptions) ?? new List<NoteModel>();
    }

    public async Task<(NoteModel Note, string Error)> CreateNoteAsync(Guid sessionId, CreateNoteModel model)
    {
        var response = await _http.PostAsJsonAsync($"/api/sessions/{sessionId}/notes", model);
        var content = await response.Content.ReadAsStringAsync();
        return ParseResponse(content);
    }

    public async Task<(NoteModel Note, string Error)> UpdateNoteAsync(Guid noteId, UpdateNoteModel model)
    {
        var response = await _http.PutAsJsonAsync($"/api/notes/{noteId}", model);
        var content = await response.Content.ReadAsStringAsync();
        return ParseResponse(content);
    }

    public async Task<(NoteModel Note, string Error)> GetNoteByIdAsync(Guid noteId)
    {
        var response = await _http.GetAsync($"/api/notes/{noteId}");
        var content = await response.Content.ReadAsStringAsync();
        return ParseResponse(content);
    }

    public async Task<(NoteModel Note, string Error)> DeleteNoteAsync(Guid noteId)
    {
        var response = await _http.DeleteAsync($"/api/notes/{noteId}");
        var content = await response.Content.ReadAsStringAsync();
        return ParseResponse(content);
    }

    public async Task<PagedResult<NoteModel>> GetClientNotesAsync(Guid clientId, DateTime? dateFrom, DateTime? dateTo, int page, int pageSize)
    {
        var url = $"/api/clients/{clientId}/notes?page={page}&page_size={pageSize}";
        if (dateFrom.HasValue)
            url += $"&date_from={dateFrom.Value:yyyy-MM-dd}";
        if (dateTo.HasValue)
            url += $"&date_to={dateTo.Value:yyyy-MM-dd}";

        var response = await _http.GetAsync(url);
        var content = await response.Content.ReadAsStringAsync();

        if (IsProblemDetails(content))
            return null;

        return JsonSerializer.Deserialize<PagedResult<NoteModel>>(content, JsonOptions) ?? new PagedResult<NoteModel>();
    }

    private static (NoteModel Note, string Error) ParseResponse(string content)
    {
        if (IsProblemDetails(content))
        {
            try
            {
                var doc = JsonDocument.Parse(content);
                var root = doc.RootElement;
                var status = root.TryGetProperty("status", out var s) || root.TryGetProperty("Status", out s)
                    ? s.GetInt32()
                    : 0;

                if (status == 403)
                    return (null, "access-denied");

                if (root.TryGetProperty("Detail", out var detail))
                    return (null, detail.GetString() ?? "An error occurred.");
                if (root.TryGetProperty("detail", out var detailLower))
                    return (null, detailLower.GetString() ?? "An error occurred.");
            }
            catch { }
            return (null, "An error occurred.");
        }
        var note = JsonSerializer.Deserialize<NoteModel>(content, JsonOptions);
        return (note, null);
    }
}
