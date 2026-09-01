using System.Net.Http.Json;
using System.Text.Json;
using MindNova.Web.Models;

using static MindNova.Web.Services.ProblemDetailsDetection;

namespace MindNova.Web.Services;

public class SessionApiService
{
    private readonly HttpClient _http;
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public SessionApiService(HttpClient http)
    {
        _http = http;
    }

    public async Task<PagedResult<SessionModel>> GetSessionsAsync(
        string therapistId, Guid? clientId, string status,
        DateTime? dateFrom, DateTime? dateTo, int page, int pageSize)
    {
        var url = $"/api/sessions?page={page}&page_size={pageSize}";
        if (!string.IsNullOrWhiteSpace(therapistId))
            url += $"&therapist_id={Uri.EscapeDataString(therapistId)}";
        if (clientId.HasValue)
            url += $"&client_id={clientId.Value}";
        if (!string.IsNullOrWhiteSpace(status))
            url += $"&status={Uri.EscapeDataString(status)}";
        if (dateFrom.HasValue)
            url += $"&date_from={dateFrom.Value:yyyy-MM-dd}";
        if (dateTo.HasValue)
            url += $"&date_to={dateTo.Value:yyyy-MM-dd}";

        var response = await _http.GetAsync(url);
        var content = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<PagedResult<SessionModel>>(content, JsonOptions) ?? new PagedResult<SessionModel>();
    }

    public async Task<SessionModel> GetSessionAsync(Guid id)
    {
        var response = await _http.GetAsync($"/api/sessions/{id}");
        var content = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<SessionModel>(content, JsonOptions);
    }

    public async Task<(SessionModel Session, string Error)> CreateSessionAsync(CreateSessionModel model)
    {
        var response = await _http.PostAsJsonAsync("/api/sessions", model);
        var content = await response.Content.ReadAsStringAsync();
        return ParseResponse(content);
    }

    public async Task<(SessionModel Session, string Error)> UpdateSessionAsync(Guid id, UpdateSessionModel model)
    {
        var response = await _http.PutAsJsonAsync($"/api/sessions/{id}", model);
        var content = await response.Content.ReadAsStringAsync();
        return ParseResponse(content);
    }

    public async Task<CalendarResponseModel> GetCalendarAsync(Guid therapistProfileId, DateTime dateFrom, DateTime dateTo)
    {
        var url = $"/api/therapists/{therapistProfileId}/calendar?date_from={dateFrom:yyyy-MM-dd}&date_to={dateTo:yyyy-MM-dd}";
        var response = await _http.GetAsync(url);
        var content = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<CalendarResponseModel>(content, JsonOptions) ?? new CalendarResponseModel();
    }

    private static (SessionModel Session, string Error) ParseResponse(string content)
    {
        if (IsProblemDetails(content))
        {
            var error = ExtractConflictMessage(content);
            return (null, error);
        }
        var session = JsonSerializer.Deserialize<SessionModel>(content, JsonOptions);
        return (session, null);
    }

    private static string ExtractConflictMessage(string json)
    {
        try
        {
            var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            if (root.TryGetProperty("type", out var typeEl) || root.TryGetProperty("Type", out typeEl))
            {
                var type = typeEl.GetString() ?? "";
                if (type.Contains("session-conflict"))
                    return "This time overlaps an existing session for the selected therapist.";
                if (type.Contains("outside-availability"))
                    return "The selected time is outside the therapist's availability.";
            }

            if (root.TryGetProperty("Detail", out var detail))
                return detail.GetString() ?? "An error occurred.";
            if (root.TryGetProperty("detail", out var detailLower))
                return detailLower.GetString() ?? "An error occurred.";
        }
        catch { }
        return "An error occurred.";
    }
}
