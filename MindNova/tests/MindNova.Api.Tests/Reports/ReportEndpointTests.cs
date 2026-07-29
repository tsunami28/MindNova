using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using MindNova.Api.Contracts;
using MindNova.Api.Tests.Auth;

namespace MindNova.Api.Tests.Reports;

[Collection("SqlServer")]
public class ReportEndpointTests
{
    private readonly SqlServerFixture _fixture;
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public ReportEndpointTests(SqlServerFixture fixture)
    {
        _fixture = fixture;
    }

    private class TokenResponse
    {
        public string Token { get; set; } = string.Empty;
    }

    private async Task<string> GetUserIdAsync(string email)
    {
        using var scope = _fixture.Factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<Microsoft.AspNetCore.Identity.UserManager<MindNova.Domain.Entities.ApplicationUser>>();
        var user = await userManager.FindByEmailAsync(email);
        return user.Id;
    }

    private async Task<(HttpClient Client, string UserId)> RegisterAndLoginAsync(string prefix = "report")
    {
        var httpClient = _fixture.Factory.CreateClient();
        var email = $"{prefix}-{Guid.NewGuid():N}@example.com";
        await httpClient.PostAsJsonAsync("/api/auth/register", new { Email = email, Password = "Test1234!" });
        var loginResponse = await httpClient.PostAsJsonAsync("/api/auth/login", new { Email = email, Password = "Test1234!" });
        var loginBody = await loginResponse.Content.ReadFromJsonAsync<TokenResponse>(JsonOptions);
        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", loginBody.Token);
        var userId = await GetUserIdAsync(email);
        return (httpClient, userId);
    }

    private async Task<Guid> CreateClientAsync(HttpClient client)
    {
        var request = new CreateClientRequest
        {
            FirstName = "Report",
            LastName = "Test",
            Email = $"rptclient-{Guid.NewGuid():N}@example.com",
            DateOfBirth = new DateTime(1990, 1, 1),
            Phone = "+31600000000"
        };
        var response = await client.PostAsJsonAsync("/api/clients", request);
        var body = await response.Content.ReadFromJsonAsync<ClientResponse>(JsonOptions);
        return body.Id;
    }

    private async Task CreateSessionWithStatusAsync(HttpClient client, Guid clientId, string therapistUserId, DateTime scheduledAt, string targetStatus)
    {
        var request = new CreateSessionRequest
        {
            ClientId = clientId,
            TherapistUserId = therapistUserId,
            ScheduledAt = scheduledAt,
            DurationMinutes = 50,
            SessionType = "Individual",
            Notes = "Report test"
        };
        var createResponse = await client.PostAsJsonAsync("/api/sessions", request);
        var session = await createResponse.Content.ReadFromJsonAsync<SessionResponse>(JsonOptions);

        if (targetStatus != "Scheduled")
        {
            var updateRequest = new UpdateSessionRequest
            {
                ScheduledAt = scheduledAt,
                DurationMinutes = 50,
                SessionType = "Individual",
                Status = targetStatus,
                Notes = "Report test"
            };
            await client.PutAsJsonAsync($"/api/sessions/{session.Id}", updateRequest);
        }
    }

    [Fact]
    [Trait("Story", "MN-36")]
    [Trait("AC", "AC-1")]
    public async Task Get_PracticeStats_ReturnsResponse()
    {
        var (client, userId) = await RegisterAndLoginAsync();
        var clientId = await CreateClientAsync(client);
        var date = DateTime.UtcNow.Date.AddDays(-3);
        await CreateSessionWithStatusAsync(client, clientId, userId, date, "Completed");

        var dateFrom = date.AddDays(-1).ToString("yyyy-MM-dd");
        var dateTo = DateTime.UtcNow.Date.ToString("yyyy-MM-dd");

        var response = await client.GetAsync($"/api/reports/practice-stats?date_from={dateFrom}&date_to={dateTo}");
        var body = await response.Content.ReadFromJsonAsync<PracticeStatsResponse>(JsonOptions);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.True(body.TotalSessions >= 1);

        client.Dispose();
    }

    [Fact]
    [Trait("Story", "MN-36")]
    [Trait("AC", "AC-2")]
    public async Task Get_PracticeStats_IncludesAllCounts()
    {
        var (client, userId) = await RegisterAndLoginAsync();
        var clientId = await CreateClientAsync(client);
        var date = DateTime.UtcNow.Date.AddDays(-2);

        await CreateSessionWithStatusAsync(client, clientId, userId, date, "Completed");
        await CreateSessionWithStatusAsync(client, clientId, userId, date.AddHours(2), "NoShow");
        await CreateSessionWithStatusAsync(client, clientId, userId, date.AddHours(4), "Cancelled");

        var dateFrom = date.AddDays(-1).ToString("yyyy-MM-dd");
        var dateTo = DateTime.UtcNow.Date.ToString("yyyy-MM-dd");

        var response = await client.GetAsync($"/api/reports/practice-stats?date_from={dateFrom}&date_to={dateTo}");
        var body = await response.Content.ReadFromJsonAsync<PracticeStatsResponse>(JsonOptions);

        Assert.True(body.CompletedCount >= 1);
        Assert.True(body.NoShowCount >= 1);
        Assert.True(body.CancelledCount >= 1);
        Assert.True(body.NoShowRate > 0);
        Assert.True(body.NewClientsCount >= 1);

        client.Dispose();
    }

    [Fact]
    [Trait("Story", "MN-36")]
    [Trait("AC", "AC-3")]
    public async Task Get_PracticeStats_IncludesTherapistUtilisation()
    {
        var (client, userId) = await RegisterAndLoginAsync();
        var clientId = await CreateClientAsync(client);
        var date = DateTime.UtcNow.Date.AddDays(-1);
        await CreateSessionWithStatusAsync(client, clientId, userId, date, "Completed");

        var dateFrom = date.AddDays(-1).ToString("yyyy-MM-dd");
        var dateTo = DateTime.UtcNow.Date.ToString("yyyy-MM-dd");

        var response = await client.GetAsync($"/api/reports/practice-stats?date_from={dateFrom}&date_to={dateTo}");
        var body = await response.Content.ReadFromJsonAsync<PracticeStatsResponse>(JsonOptions);

        Assert.NotNull(body.TherapistUtilisation);
        Assert.Contains(body.TherapistUtilisation, t => t.TherapistUserId == userId && t.SessionCount >= 1);

        client.Dispose();
    }

    [Fact]
    [Trait("Story", "MN-36")]
    [Trait("AC", "AC-4")]
    public async Task Get_PracticeStats_EmptyRange_ReturnsZeroCounts()
    {
        var (client, _) = await RegisterAndLoginAsync();

        var dateFrom = "2020-01-01";
        var dateTo = "2020-01-02";

        var response = await client.GetAsync($"/api/reports/practice-stats?date_from={dateFrom}&date_to={dateTo}");
        var body = await response.Content.ReadFromJsonAsync<PracticeStatsResponse>(JsonOptions);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(0, body.TotalSessions);
        Assert.Equal(0, body.CompletedCount);
        Assert.Equal(0, body.NoShowCount);
        Assert.Equal(0, body.CancelledCount);
        Assert.Equal(0.0, body.NoShowRate);
        Assert.Equal(0, body.NewClientsCount);

        client.Dispose();
    }

    [Fact]
    [Trait("Story", "MN-36")]
    [Trait("AC", "AC-5")]
    public async Task Get_PracticeStats_MissingDateParams_ReturnsProblemDetails()
    {
        var (client, _) = await RegisterAndLoginAsync();

        var response = await client.GetAsync("/api/reports/practice-stats");
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("date_from", body, StringComparison.OrdinalIgnoreCase);

        client.Dispose();
    }

    [Fact]
    [Trait("Story", "MN-36")]
    [Trait("AC", "AC-6")]
    public async Task Get_PracticeStats_Unauthenticated_Returns401()
    {
        var unauthClient = _fixture.Factory.CreateClient();

        var response = await unauthClient.GetAsync("/api/reports/practice-stats?date_from=2026-01-01&date_to=2026-01-31");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);

        unauthClient.Dispose();
    }
}
