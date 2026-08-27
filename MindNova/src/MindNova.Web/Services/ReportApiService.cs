using System.Text.Json;
using MindNova.Web.Models;

namespace MindNova.Web.Services;

public class ReportApiService
{
    private readonly HttpClient _http;
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public ReportApiService(HttpClient http)
    {
        _http = http;
    }

    public async Task<PracticeStatsModel> GetPracticeStatsAsync(DateTime dateFrom, DateTime dateTo)
    {
        var url = $"/api/reports/practice-stats?date_from={dateFrom:yyyy-MM-dd}&date_to={dateTo:yyyy-MM-dd}";
        var response = await _http.GetAsync(url);
        var content = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<PracticeStatsModel>(content, JsonOptions) ?? new PracticeStatsModel();
    }

    public async Task<TherapistStatsModel> GetTherapistStatsAsync(DateTime dateFrom, DateTime dateTo)
    {
        var url = $"/api/reports/therapist-stats?date_from={dateFrom:yyyy-MM-dd}&date_to={dateTo:yyyy-MM-dd}";
        var response = await _http.GetAsync(url);
        var content = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<TherapistStatsModel>(content, JsonOptions) ?? new TherapistStatsModel();
    }

    public string GetPracticeStatsCsvUrl(DateTime dateFrom, DateTime dateTo) =>
        $"/api/reports/practice-stats/export?date_from={dateFrom:yyyy-MM-dd}&date_to={dateTo:yyyy-MM-dd}";

    public string GetTherapistStatsCsvUrl(DateTime dateFrom, DateTime dateTo) =>
        $"/api/reports/therapist-stats/export?date_from={dateFrom:yyyy-MM-dd}&date_to={dateTo:yyyy-MM-dd}";
}
