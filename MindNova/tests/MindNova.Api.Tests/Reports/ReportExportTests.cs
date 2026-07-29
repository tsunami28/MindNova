using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using MindNova.Api.Contracts;
using MindNova.Api.Tests.Auth;

namespace MindNova.Api.Tests.Reports;

[Collection("SqlServer")]
public class ReportExportTests
{
    private readonly SqlServerFixture _fixture;
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public ReportExportTests(SqlServerFixture fixture)
    {
        _fixture = fixture;
    }

    private class TokenResponse
    {
        public string Token { get; set; } = string.Empty;
    }

    private async Task<HttpClient> RegisterAndLoginAsync()
    {
        var httpClient = _fixture.Factory.CreateClient();
        var email = $"export-{Guid.NewGuid():N}@example.com";
        await httpClient.PostAsJsonAsync("/api/auth/register", new { Email = email, Password = "Test1234!" });
        var loginResponse = await httpClient.PostAsJsonAsync("/api/auth/login", new { Email = email, Password = "Test1234!" });
        var loginBody = await loginResponse.Content.ReadFromJsonAsync<TokenResponse>(JsonOptions);
        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", loginBody.Token);
        return httpClient;
    }

    [Fact]
    [Trait("Story", "MN-38")]
    [Trait("AC", "AC-1")]
    public async Task Export_PracticeStats_ReturnsCsv()
    {
        var client = await RegisterAndLoginAsync();

        var response = await client.GetAsync("/api/reports/practice-stats/export?date_from=2020-01-01&date_to=2020-01-31");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("text/csv", response.Content.Headers.ContentType.MediaType);

        var csv = await response.Content.ReadAsStringAsync();
        Assert.Contains("TotalSessions", csv);
        Assert.Contains("CompletedCount", csv);
        Assert.Contains("NoShowRate", csv);

        client.Dispose();
    }

    [Fact]
    [Trait("Story", "MN-38")]
    [Trait("AC", "AC-2")]
    public async Task Export_TherapistStats_ReturnsCsv()
    {
        var client = await RegisterAndLoginAsync();

        var response = await client.GetAsync("/api/reports/therapist-stats/export?date_from=2020-01-01&date_to=2020-01-31");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("text/csv", response.Content.Headers.ContentType.MediaType);

        var csv = await response.Content.ReadAsStringAsync();
        Assert.Contains("TherapistUserId", csv);
        Assert.Contains("UtilisationRate", csv);

        client.Dispose();
    }

    [Fact]
    [Trait("Story", "MN-38")]
    [Trait("AC", "AC-3")]
    public async Task Export_PracticeStats_HeaderRowIsPascalCase()
    {
        var client = await RegisterAndLoginAsync();

        var response = await client.GetAsync("/api/reports/practice-stats/export?date_from=2020-01-01&date_to=2020-01-31");
        var csv = await response.Content.ReadAsStringAsync();
        var headerLine = csv.Split('\n')[0].Trim();

        Assert.Contains("DateFrom", headerLine);
        Assert.Contains("DateTo", headerLine);
        Assert.Contains("TotalSessions", headerLine);
        Assert.Contains("NewClientsCount", headerLine);

        client.Dispose();
    }

    [Fact]
    [Trait("Story", "MN-38")]
    [Trait("AC", "AC-4")]
    public async Task Export_PracticeStats_HasContentDisposition()
    {
        var client = await RegisterAndLoginAsync();

        var response = await client.GetAsync("/api/reports/practice-stats/export?date_from=2026-07-01&date_to=2026-07-28");

        Assert.NotNull(response.Content.Headers.ContentDisposition);
        Assert.Equal("attachment", response.Content.Headers.ContentDisposition.DispositionType);
        Assert.Contains("practice-stats", response.Content.Headers.ContentDisposition.FileName);

        client.Dispose();
    }

    [Fact]
    [Trait("Story", "MN-38")]
    [Trait("AC", "AC-5")]
    public async Task Export_Unauthenticated_Returns401()
    {
        var unauthClient = _fixture.Factory.CreateClient();

        var practiceResponse = await unauthClient.GetAsync("/api/reports/practice-stats/export?date_from=2026-01-01&date_to=2026-01-31");
        var therapistResponse = await unauthClient.GetAsync("/api/reports/therapist-stats/export?date_from=2026-01-01&date_to=2026-01-31");

        Assert.Equal(HttpStatusCode.Unauthorized, practiceResponse.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, therapistResponse.StatusCode);

        unauthClient.Dispose();
    }
}
