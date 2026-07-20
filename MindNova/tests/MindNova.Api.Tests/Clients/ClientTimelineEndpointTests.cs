using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using MindNova.Api.Contracts;
using MindNova.Api.Tests.Auth;

namespace MindNova.Api.Tests.Clients;

[Collection("SqlServer")]
public class ClientTimelineEndpointTests
{
    private readonly SqlServerFixture _fixture;
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public ClientTimelineEndpointTests(SqlServerFixture fixture)
    {
        _fixture = fixture;
    }

    private class TokenResponse
    {
        public string Token { get; set; } = string.Empty;
    }

    private async Task<HttpClient> CreateAuthenticatedClientAsync()
    {
        var httpClient = _fixture.Factory.CreateClient();
        var email = $"timeline-{Guid.NewGuid():N}@example.com";
        await httpClient.PostAsJsonAsync("/api/auth/register", new { Email = email, Password = "Test1234!" });
        var loginResponse = await httpClient.PostAsJsonAsync("/api/auth/login", new { Email = email, Password = "Test1234!" });
        var loginBody = await loginResponse.Content.ReadFromJsonAsync<TokenResponse>(JsonOptions);
        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", loginBody.Token);
        return httpClient;
    }

    private async Task<Guid> CreateClientAsync(HttpClient client)
    {
        var request = new CreateClientRequest
        {
            FirstName = "Timeline",
            LastName = $"Test-{Guid.NewGuid():N}",
            Email = $"tl-{Guid.NewGuid():N}@example.com",
            DateOfBirth = new DateTime(1985, 3, 15),
            Phone = "+31600000001"
        };
        var response = await client.PostAsJsonAsync("/api/clients", request);
        var body = await response.Content.ReadFromJsonAsync<ClientResponse>(JsonOptions);
        return body.Id;
    }

    private async Task<Guid> CreateSessionForClientAsync(HttpClient client, Guid clientId, string therapistId, DateTime scheduledAt)
    {
        var request = new CreateSessionRequest
        {
            ClientId = clientId,
            TherapistUserId = therapistId,
            ScheduledAt = scheduledAt,
            DurationMinutes = 50,
            SessionType = "Individual",
            Notes = "Timeline test session"
        };
        var response = await client.PostAsJsonAsync("/api/sessions", request);
        var body = await response.Content.ReadFromJsonAsync<SessionResponse>(JsonOptions);
        return body.Id;
    }

    private async Task<string> GetUserIdAsync(string email)
    {
        using var scope = _fixture.Factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<Microsoft.AspNetCore.Identity.UserManager<MindNova.Domain.Entities.ApplicationUser>>();
        var user = await userManager.FindByEmailAsync(email);
        return user.Id;
    }

    private async Task<(HttpClient Client, string TherapistId, Guid ClientId)> SetupWithClientAsync()
    {
        var httpClient = _fixture.Factory.CreateClient();
        var email = $"therapist-tl-{Guid.NewGuid():N}@example.com";
        await httpClient.PostAsJsonAsync("/api/auth/register", new { Email = email, Password = "Test1234!" });
        var loginResponse = await httpClient.PostAsJsonAsync("/api/auth/login", new { Email = email, Password = "Test1234!" });
        var loginBody = await loginResponse.Content.ReadFromJsonAsync<TokenResponse>(JsonOptions);
        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", loginBody.Token);

        var therapistId = await GetUserIdAsync(email);
        var clientId = await CreateClientAsync(httpClient);

        return (httpClient, therapistId, clientId);
    }

    [Fact]
    [Trait("Story", "MN-16")]
    [Trait("AC", "AC-1")]
    public async Task GetTimeline_WithSessions_ReturnsEventsOrderedByDateDescending()
    {
        var (client, therapistId, clientId) = await SetupWithClientAsync();

        var older = DateTime.UtcNow.AddDays(-3);
        var newer = DateTime.UtcNow.AddDays(-1);
        await CreateSessionForClientAsync(client, clientId, therapistId, older);
        await CreateSessionForClientAsync(client, clientId, therapistId, newer);

        var response = await client.GetAsync($"/api/clients/{clientId}/timeline");
        var body = await response.Content.ReadFromJsonAsync<PagedResponse<TimelineEventResponse>>(JsonOptions);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(2, body.Items.Count);
        Assert.True(body.Items[0].Date >= body.Items[1].Date);
    }

    [Fact]
    [Trait("Story", "MN-16")]
    [Trait("AC", "AC-2")]
    public async Task GetTimeline_EventHasAllRequiredProperties()
    {
        var (client, therapistId, clientId) = await SetupWithClientAsync();
        await CreateSessionForClientAsync(client, clientId, therapistId, DateTime.UtcNow.AddDays(-1));

        var response = await client.GetAsync($"/api/clients/{clientId}/timeline");
        var body = await response.Content.ReadFromJsonAsync<PagedResponse<TimelineEventResponse>>(JsonOptions);

        var evt = body.Items[0];
        Assert.NotEqual(default, evt.Date);
        Assert.False(string.IsNullOrEmpty(evt.EventType));
        Assert.False(string.IsNullOrEmpty(evt.Summary));
        Assert.NotEqual(Guid.Empty, evt.SourceId);
    }

    [Fact]
    [Trait("Story", "MN-16")]
    [Trait("AC", "AC-3")]
    public async Task GetTimeline_SessionEvent_HasCorrectFieldValues()
    {
        var (client, therapistId, clientId) = await SetupWithClientAsync();
        var scheduledAt = DateTime.UtcNow.AddDays(-2);
        var sessionId = await CreateSessionForClientAsync(client, clientId, therapistId, scheduledAt);

        var response = await client.GetAsync($"/api/clients/{clientId}/timeline");
        var body = await response.Content.ReadFromJsonAsync<PagedResponse<TimelineEventResponse>>(JsonOptions);

        var evt = body.Items[0];
        Assert.Equal("Session", evt.EventType);
        Assert.Equal(sessionId, evt.SourceId);
        Assert.Contains("Individual", evt.Summary);
        Assert.Contains("Scheduled", evt.Summary);
        Assert.Equal(scheduledAt, evt.Date, TimeSpan.FromSeconds(1));
    }

    [Fact]
    [Trait("Story", "MN-16")]
    [Trait("AC", "AC-4")]
    public async Task GetTimeline_NoSessions_ReturnsEmptyList()
    {
        var (client, _, clientId) = await SetupWithClientAsync();

        var response = await client.GetAsync($"/api/clients/{clientId}/timeline");
        var body = await response.Content.ReadFromJsonAsync<PagedResponse<TimelineEventResponse>>(JsonOptions);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Empty(body.Items);
        Assert.Equal(0, body.TotalCount);
    }

    [Fact]
    [Trait("Story", "MN-16")]
    [Trait("AC", "AC-5")]
    public async Task GetTimeline_NonExistentClient_ReturnsProblemDetails()
    {
        var client = await CreateAuthenticatedClientAsync();
        var fakeId = Guid.NewGuid();

        var response = await client.GetAsync($"/api/clients/{fakeId}/timeline");
        var body = await response.Content.ReadFromJsonAsync<ProblemDetails>(JsonOptions);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(404, body.Status);
        Assert.Contains("not found", body.Title, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    [Trait("Story", "MN-16")]
    [Trait("AC", "AC-6")]
    public async Task GetTimeline_PaginationDefaults_ApplyCorrectly()
    {
        var (client, _, clientId) = await SetupWithClientAsync();

        var response = await client.GetAsync($"/api/clients/{clientId}/timeline");
        var body = await response.Content.ReadFromJsonAsync<PagedResponse<TimelineEventResponse>>(JsonOptions);

        Assert.Equal(1, body.Page);
        Assert.Equal(20, body.PageSize);
    }

    [Fact]
    [Trait("Story", "MN-16")]
    [Trait("AC", "AC-6")]
    public async Task GetTimeline_PageSizeClampedToMax100()
    {
        var (client, _, clientId) = await SetupWithClientAsync();

        var response = await client.GetAsync($"/api/clients/{clientId}/timeline?page_size=200");
        var body = await response.Content.ReadFromJsonAsync<PagedResponse<TimelineEventResponse>>(JsonOptions);

        Assert.Equal(100, body.PageSize);
    }

    [Fact]
    [Trait("Story", "MN-16")]
    [Trait("AC", "AC-6")]
    public async Task GetTimeline_PageSizeClampedToMin1()
    {
        var (client, _, clientId) = await SetupWithClientAsync();

        var response = await client.GetAsync($"/api/clients/{clientId}/timeline?page_size=0");
        var body = await response.Content.ReadFromJsonAsync<PagedResponse<TimelineEventResponse>>(JsonOptions);

        Assert.Equal(1, body.PageSize);
    }

    [Fact]
    [Trait("Story", "MN-16")]
    [Trait("AC", "AC-7")]
    public async Task GetTimeline_Pagination_ReturnsCorrectPage()
    {
        var (client, therapistId, clientId) = await SetupWithClientAsync();

        for (int i = 0; i < 5; i++)
        {
            await CreateSessionForClientAsync(client, clientId, therapistId, DateTime.UtcNow.AddDays(-i));
        }

        var response = await client.GetAsync($"/api/clients/{clientId}/timeline?page=2&page_size=2");
        var body = await response.Content.ReadFromJsonAsync<PagedResponse<TimelineEventResponse>>(JsonOptions);

        Assert.Equal(5, body.TotalCount);
        Assert.Equal(2, body.Items.Count);
        Assert.Equal(2, body.Page);
        Assert.Equal(2, body.PageSize);
    }

    [Fact]
    [Trait("Story", "MN-16")]
    [Trait("AC", "AC-8")]
    public async Task GetTimeline_ArchivedClient_StillReturnsTimeline()
    {
        var (client, therapistId, clientId) = await SetupWithClientAsync();
        await CreateSessionForClientAsync(client, clientId, therapistId, DateTime.UtcNow.AddDays(-1));

        await client.DeleteAsync($"/api/clients/{clientId}");

        var response = await client.GetAsync($"/api/clients/{clientId}/timeline");
        var body = await response.Content.ReadFromJsonAsync<PagedResponse<TimelineEventResponse>>(JsonOptions);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Single(body.Items);
    }

    [Fact]
    [Trait("Story", "MN-16")]
    [Trait("AC", "AC-9")]
    public async Task GetTimeline_Unauthenticated_Returns401()
    {
        var unauthClient = _fixture.Factory.CreateClient();

        var response = await unauthClient.GetAsync($"/api/clients/{Guid.NewGuid()}/timeline");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
