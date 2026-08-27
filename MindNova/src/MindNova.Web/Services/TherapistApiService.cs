using System.Net.Http.Json;
using System.Text.Json;
using MindNova.Web.Models;

namespace MindNova.Web.Services;

public class TherapistApiService
{
    private readonly HttpClient _http;
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public TherapistApiService(HttpClient http)
    {
        _http = http;
    }

    public async Task<PagedResult<TherapistModel>> GetTherapistsAsync(int page, int pageSize, bool includeInactive)
    {
        var url = $"/api/therapists?page={page}&page_size={pageSize}";
        if (includeInactive)
            url += "&include_inactive=true";
        var response = await _http.GetAsync(url);
        var content = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<PagedResult<TherapistModel>>(content, JsonOptions) ?? new PagedResult<TherapistModel>();
    }

    public async Task<TherapistModel> GetTherapistAsync(Guid id)
    {
        var response = await _http.GetAsync($"/api/therapists/{id}");
        var content = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<TherapistModel>(content, JsonOptions);
    }

    public async Task<(TherapistModel Profile, string Error)> CreateTherapistAsync(CreateTherapistModel model)
    {
        var response = await _http.PostAsJsonAsync("/api/therapists", model);
        var content = await response.Content.ReadAsStringAsync();
        if (content.Contains("\"Status\"") && content.Contains("\"Title\""))
            return (null, ExtractDetail(content));
        return (JsonSerializer.Deserialize<TherapistModel>(content, JsonOptions), null);
    }

    public async Task<(TherapistModel Profile, string Error)> UpdateTherapistAsync(Guid id, UpdateTherapistModel model)
    {
        var response = await _http.PutAsJsonAsync($"/api/therapists/{id}", model);
        var content = await response.Content.ReadAsStringAsync();
        if (content.Contains("\"Status\"") && content.Contains("\"Title\""))
            return (null, ExtractDetail(content));
        return (JsonSerializer.Deserialize<TherapistModel>(content, JsonOptions), null);
    }

    public async Task<TherapistModel> DeactivateAsync(Guid id)
    {
        var response = await _http.DeleteAsync($"/api/therapists/{id}");
        var content = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<TherapistModel>(content, JsonOptions);
    }

    public async Task<List<AvailabilitySlotModel>> GetSlotsAsync(Guid therapistProfileId)
    {
        var response = await _http.GetAsync($"/api/therapists/{therapistProfileId}/availability");
        var content = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<List<AvailabilitySlotModel>>(content, JsonOptions) ?? new List<AvailabilitySlotModel>();
    }

    public async Task<(AvailabilitySlotModel Slot, string Error)> CreateSlotAsync(Guid therapistProfileId, CreateSlotModel model)
    {
        var response = await _http.PostAsJsonAsync($"/api/therapists/{therapistProfileId}/availability", model);
        var content = await response.Content.ReadAsStringAsync();
        if (content.Contains("\"Status\"") && content.Contains("\"Title\""))
            return (null, ExtractDetail(content));
        return (JsonSerializer.Deserialize<AvailabilitySlotModel>(content, JsonOptions), null);
    }

    public async Task DeleteSlotAsync(Guid slotId)
    {
        await _http.DeleteAsync($"/api/availability/{slotId}");
    }

    private static string ExtractDetail(string json)
    {
        try
        {
            var doc = JsonDocument.Parse(json);
            if (doc.RootElement.TryGetProperty("Detail", out var d)) return d.GetString() ?? "An error occurred.";
            if (doc.RootElement.TryGetProperty("detail", out var dl)) return dl.GetString() ?? "An error occurred.";
        }
        catch { }
        return "An error occurred.";
    }
}
