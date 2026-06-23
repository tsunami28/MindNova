using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using MindNova.Api.Contracts;
using MindNova.Api.Tests.Auth;

namespace MindNova.Api.Tests.Sessions;

[Collection("SqlServer")]
public class SessionFilterTests
{
    private readonly SqlServerFixture _fixture;
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public SessionFilterTests(SqlServerFixture fixture)
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

    private async Task<(HttpClient Client, Guid ClientId, string TherapistUserId)> SetupWithClientAsync()
    {
        var email = $"filter-{Guid.NewGuid():N}@example.com";
        await _fixture.Client.PostAsJsonAsync("/api/auth/register", new { Email = email, Password = "Test1234!" });
        var loginResponse = await _fixture.Client.PostAsJsonAsync("/api/auth/login", new { Email = email, Password = "Test1234!" });
        var loginBody = await loginResponse.Content.ReadFromJsonAsync<TokenResponse>(JsonOptions);

        var client = _fixture.Factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", loginBody.Token);

        var therapistId = await GetUserIdAsync(email);

        var clientRequest = new CreateClientRequest
        {
            FirstName = "Filter",
            LastName = "Test",
            Email = $"filterclient-{Guid.NewGuid():N}@example.com"
        };
        var createClientResponse = await client.PostAsJsonAsync("/api/clients", clientRequest);
        var createdClient = await createClientResponse.Content.ReadFromJsonAsync<ClientResponse>(JsonOptions);

        return (client, createdClient.Id, therapistId);
    }

    private async Task<SessionResponse> CreateSessionAsync(HttpClient client, Guid clientId, string therapistUserId, DateTime scheduledAt, string sessionType = "Individual")
    {
        var request = new CreateSessionRequest
        {
            ClientId = clientId,
            TherapistUserId = therapistUserId,
            ScheduledAt = scheduledAt,
            DurationMinutes = 50,
            SessionType = sessionType,
            Notes = "Test session"
        };
        var response = await client.PostAsJsonAsync("/api/sessions", request);
        return await response.Content.ReadFromJsonAsync<SessionResponse>(JsonOptions);
    }

    [Fact]
    [Trait("Story", "MN-19")]
    [Trait("AC", "AC-1")]
    public async Task FilterByClientId_ReturnsOnlyMatchingSessions()
    {
        var (client, clientId, therapistId) = await SetupWithClientAsync();

        // Create a second client
        var client2Request = new CreateClientRequest
        {
            FirstName = "Other",
            LastName = "Client",
            Email = $"other-{Guid.NewGuid():N}@example.com"
        };
        var client2Response = await client.PostAsJsonAsync("/api/clients", client2Request);
        var client2 = await client2Response.Content.ReadFromJsonAsync<ClientResponse>(JsonOptions);

        await CreateSessionAsync(client, clientId, therapistId, DateTime.UtcNow.AddDays(1));
        await CreateSessionAsync(client, client2.Id, therapistId, DateTime.UtcNow.AddDays(2));

        var response = await client.GetAsync($"/api/sessions?client_id={clientId}");
        var body = await response.Content.ReadFromJsonAsync<PagedResponse<SessionResponse>>(JsonOptions);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.All(body.Items, s => Assert.Equal(clientId, s.ClientId));
    }

    [Fact]
    [Trait("Story", "MN-19")]
    [Trait("AC", "AC-2")]
    public async Task FilterByTherapistId_ReturnsOnlyMatchingSessions()
    {
        var (client, clientId, therapistId) = await SetupWithClientAsync();
        await CreateSessionAsync(client, clientId, therapistId, DateTime.UtcNow.AddDays(1));

        var response = await client.GetAsync($"/api/sessions?therapist_id={therapistId}");
        var body = await response.Content.ReadFromJsonAsync<PagedResponse<SessionResponse>>(JsonOptions);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.All(body.Items, s => Assert.Equal(therapistId, s.TherapistUserId));
    }

    [Fact]
    [Trait("Story", "MN-19")]
    [Trait("AC", "AC-3")]
    public async Task FilterByStatus_ReturnsOnlyMatchingSessions()
    {
        var (client, clientId, therapistId) = await SetupWithClientAsync();
        var session = await CreateSessionAsync(client, clientId, therapistId, DateTime.UtcNow.AddDays(1));

        // Complete one session
        await client.PutAsJsonAsync($"/api/sessions/{session.Id}", new UpdateSessionRequest
        {
            ScheduledAt = session.ScheduledAt,
            DurationMinutes = session.DurationMinutes,
            SessionType = session.SessionType,
            Status = "Completed",
            Notes = session.Notes
        });

        // Create another that stays Scheduled
        await CreateSessionAsync(client, clientId, therapistId, DateTime.UtcNow.AddDays(3));

        var response = await client.GetAsync("/api/sessions?status=Completed");
        var body = await response.Content.ReadFromJsonAsync<PagedResponse<SessionResponse>>(JsonOptions);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.All(body.Items, s => Assert.Equal("Completed", s.Status));
    }

    [Fact]
    [Trait("Story", "MN-19")]
    [Trait("AC", "AC-4")]
    public async Task FilterByDateRange_ReturnsOnlySessionsInRange()
    {
        var (client, clientId, therapistId) = await SetupWithClientAsync();
        var inRange = DateTime.UtcNow.AddDays(5);
        var outOfRange = DateTime.UtcNow.AddDays(30);

        await CreateSessionAsync(client, clientId, therapistId, inRange);
        await CreateSessionAsync(client, clientId, therapistId, outOfRange);

        var from = DateTime.UtcNow.AddDays(4).ToString("yyyy-MM-dd");
        var to = DateTime.UtcNow.AddDays(6).ToString("yyyy-MM-dd");

        var response = await client.GetAsync($"/api/sessions?date_from={from}&date_to={to}&client_id={clientId}");
        var body = await response.Content.ReadFromJsonAsync<PagedResponse<SessionResponse>>(JsonOptions);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Single(body.Items);
    }

    [Fact]
    [Trait("Story", "MN-19")]
    [Trait("AC", "AC-5")]
    public async Task CombinedFilters_ApplyAllConditions()
    {
        var (client, clientId, therapistId) = await SetupWithClientAsync();
        await CreateSessionAsync(client, clientId, therapistId, DateTime.UtcNow.AddDays(10));
        var session2 = await CreateSessionAsync(client, clientId, therapistId, DateTime.UtcNow.AddDays(11));

        // Complete the second session
        await client.PutAsJsonAsync($"/api/sessions/{session2.Id}", new UpdateSessionRequest
        {
            ScheduledAt = session2.ScheduledAt,
            DurationMinutes = session2.DurationMinutes,
            SessionType = session2.SessionType,
            Status = "Completed",
            Notes = session2.Notes
        });

        // Filter: client + status=Scheduled
        var response = await client.GetAsync($"/api/sessions?client_id={clientId}&status=Scheduled");
        var body = await response.Content.ReadFromJsonAsync<PagedResponse<SessionResponse>>(JsonOptions);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.All(body.Items, s =>
        {
            Assert.Equal(clientId, s.ClientId);
            Assert.Equal("Scheduled", s.Status);
        });
    }

    [Fact]
    [Trait("Story", "MN-19")]
    [Trait("AC", "AC-6")]
    public async Task Pagination_ReturnsCorrectMetadata()
    {
        var (client, clientId, therapistId) = await SetupWithClientAsync();
        for (int i = 0; i < 5; i++)
            await CreateSessionAsync(client, clientId, therapistId, DateTime.UtcNow.AddDays(20 + i));

        var response = await client.GetAsync($"/api/sessions?client_id={clientId}&page=2&page_size=2");
        var body = await response.Content.ReadFromJsonAsync<PagedResponse<SessionResponse>>(JsonOptions);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(2, body.Page);
        Assert.Equal(2, body.PageSize);
        Assert.Equal(5, body.TotalCount);
        Assert.Equal(2, body.Items.Count);
    }

    [Fact]
    [Trait("Story", "MN-19")]
    [Trait("AC", "AC-7")]
    public async Task NoMatch_ReturnsEmptyItemsList()
    {
        var (client, _, _) = await SetupWithClientAsync();
        var nonExistentClientId = Guid.NewGuid();

        var response = await client.GetAsync($"/api/sessions?client_id={nonExistentClientId}");
        var body = await response.Content.ReadFromJsonAsync<PagedResponse<SessionResponse>>(JsonOptions);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Empty(body.Items);
        Assert.Equal(0, body.TotalCount);
    }
}
