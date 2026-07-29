using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using MindNova.Api.Contracts;
using MindNova.Api.Tests.Auth;

namespace MindNova.Api.Tests.Reports;

[Collection("SqlServer")]
public class TherapistStatsEndpointTests
{
    private readonly SqlServerFixture _fixture;
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public TherapistStatsEndpointTests(SqlServerFixture fixture)
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

    private async Task<(HttpClient Client, string UserId)> RegisterAndLoginAsync(string prefix = "tstats")
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
            FirstName = "TStats",
            LastName = "Test",
            Email = $"tsclient-{Guid.NewGuid():N}@example.com",
            DateOfBirth = new DateTime(1990, 1, 1),
            Phone = "+31600000000"
        };
        var response = await client.PostAsJsonAsync("/api/clients", request);
        var body = await response.Content.ReadFromJsonAsync<ClientResponse>(JsonOptions);
        return body.Id;
    }

    private async Task<Guid> CreateTherapistProfileAsync(HttpClient client, string userId)
    {
        var request = new CreateTherapistRequest
        {
            UserId = userId,
            Specialisations = new List<string> { "CBT" },
            MaxCaseload = 10
        };
        var response = await client.PostAsJsonAsync("/api/therapists", request);
        var body = await response.Content.ReadFromJsonAsync<TherapistProfileResponse>(JsonOptions);
        return body.Id;
    }

    private async Task CreateAvailabilitySlotAsync(HttpClient client, Guid profileId, DateTime date)
    {
        var request = new CreateAvailabilityRequest
        {
            DayOfWeek = null,
            SpecificDate = date.Date,
            StartTime = new TimeSpan(9, 0, 0),
            EndTime = new TimeSpan(17, 0, 0),
            IsRecurring = false
        };
        await client.PostAsJsonAsync($"/api/therapists/{profileId}/availability", request);
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
            Notes = "TStats test"
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
                Notes = "TStats test"
            };
            await client.PutAsJsonAsync($"/api/sessions/{session.Id}", updateRequest);
        }
    }

    [Fact]
    [Trait("Story", "MN-37")]
    [Trait("AC", "AC-1")]
    public async Task Get_TherapistStats_ReturnsResponse()
    {
        var (client, userId) = await RegisterAndLoginAsync();
        var clientId = await CreateClientAsync(client);
        var profileId = await CreateTherapistProfileAsync(client, userId);
        var date = DateTime.UtcNow.Date.AddDays(-3);
        await CreateAvailabilitySlotAsync(client, profileId, date);
        await CreateSessionWithStatusAsync(client, clientId, userId, date.AddHours(10), "Completed");

        var dateFrom = date.AddDays(-1).ToString("yyyy-MM-dd");
        var dateTo = DateTime.UtcNow.Date.ToString("yyyy-MM-dd");

        var response = await client.GetAsync($"/api/reports/therapist-stats?date_from={dateFrom}&date_to={dateTo}");
        var body = await response.Content.ReadFromJsonAsync<TherapistStatsResponse>(JsonOptions);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(body.Items);
        Assert.Contains(body.Items, t => t.TherapistUserId == userId);

        client.Dispose();
    }

    [Fact]
    [Trait("Story", "MN-37")]
    [Trait("AC", "AC-2")]
    public async Task Get_TherapistStats_EntryHasAllFields()
    {
        var (client, userId) = await RegisterAndLoginAsync();
        var clientId = await CreateClientAsync(client);
        var profileId = await CreateTherapistProfileAsync(client, userId);
        var date = DateTime.UtcNow.Date.AddDays(-2);
        await CreateAvailabilitySlotAsync(client, profileId, date);
        await CreateSessionWithStatusAsync(client, clientId, userId, date.AddHours(10), "Completed");
        await CreateSessionWithStatusAsync(client, clientId, userId, date.AddHours(12), "NoShow");

        var dateFrom = date.AddDays(-1).ToString("yyyy-MM-dd");
        var dateTo = DateTime.UtcNow.Date.ToString("yyyy-MM-dd");

        var response = await client.GetAsync($"/api/reports/therapist-stats?date_from={dateFrom}&date_to={dateTo}");
        var body = await response.Content.ReadFromJsonAsync<TherapistStatsResponse>(JsonOptions);

        var entry = body.Items.First(t => t.TherapistUserId == userId);
        Assert.True(entry.TotalSessions >= 2);
        Assert.True(entry.CompletedCount >= 1);
        Assert.True(entry.NoShowCount >= 1);
        Assert.NotEmpty(entry.TherapistName);

        client.Dispose();
    }

    [Fact]
    [Trait("Story", "MN-37")]
    [Trait("AC", "AC-3")]
    public async Task Get_TherapistStats_IncludesAvailableSlotCount()
    {
        var (client, userId) = await RegisterAndLoginAsync();
        var clientId = await CreateClientAsync(client);
        var profileId = await CreateTherapistProfileAsync(client, userId);
        var date = DateTime.UtcNow.Date.AddDays(-1);
        await CreateAvailabilitySlotAsync(client, profileId, date);
        await CreateSessionWithStatusAsync(client, clientId, userId, date.AddHours(10), "Completed");

        var dateFrom = date.AddDays(-1).ToString("yyyy-MM-dd");
        var dateTo = DateTime.UtcNow.Date.ToString("yyyy-MM-dd");

        var response = await client.GetAsync($"/api/reports/therapist-stats?date_from={dateFrom}&date_to={dateTo}");
        var body = await response.Content.ReadFromJsonAsync<TherapistStatsResponse>(JsonOptions);

        var entry = body.Items.First(t => t.TherapistUserId == userId);
        Assert.True(entry.AvailableSlotCount >= 1);
        Assert.True(entry.UtilisationRate > 0);

        client.Dispose();
    }

    [Fact]
    [Trait("Story", "MN-37")]
    [Trait("AC", "AC-4")]
    public async Task Get_TherapistStats_EmptyRange_ReturnsEmptyList()
    {
        var (client, _) = await RegisterAndLoginAsync();

        var response = await client.GetAsync("/api/reports/therapist-stats?date_from=2020-01-01&date_to=2020-01-02");
        var body = await response.Content.ReadFromJsonAsync<TherapistStatsResponse>(JsonOptions);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Empty(body.Items);

        client.Dispose();
    }

    [Fact]
    [Trait("Story", "MN-37")]
    [Trait("AC", "AC-5")]
    public async Task Get_TherapistStats_Unauthenticated_Returns401()
    {
        var unauthClient = _fixture.Factory.CreateClient();

        var response = await unauthClient.GetAsync("/api/reports/therapist-stats?date_from=2026-01-01&date_to=2026-01-31");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);

        unauthClient.Dispose();
    }
}
