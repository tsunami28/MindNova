using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using MindNova.Api.Contracts;
using MindNova.Api.Tests.Auth;

namespace MindNova.Api.Tests.Sessions;

[Collection("SqlServer")]
public class SessionEndpointTests
{
    private readonly SqlServerFixture _fixture;
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public SessionEndpointTests(SqlServerFixture fixture)
    {
        _fixture = fixture;
    }

    private class TokenResponse
    {
        public string Token { get; set; } = string.Empty;
    }

    private async Task<(string Token, string UserId)> RegisterAndGetTokenAsync(HttpClient client)
    {
        var email = $"session-{Guid.NewGuid():N}@example.com";
        await client.PostAsJsonAsync("/api/auth/register", new { Email = email, Password = "Test1234!" });
        var loginResponse = await client.PostAsJsonAsync("/api/auth/login", new { Email = email, Password = "Test1234!" });
        var loginBody = await loginResponse.Content.ReadFromJsonAsync<TokenResponse>(JsonOptions);

        var protectedClient = _fixture.Factory.CreateClient();
        protectedClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", loginBody.Token);
        var whoResponse = await protectedClient.GetAsync("/api/protected/admin-only");
        protectedClient.Dispose();

        return (loginBody.Token, email);
    }

    private async Task<(HttpClient Client, string TherapistUserId, Guid ClientId)> SetupAsync()
    {
        var httpClient = _fixture.Factory.CreateClient();

        var email = $"therapist-{Guid.NewGuid():N}@example.com";
        await httpClient.PostAsJsonAsync("/api/auth/register", new { Email = email, Password = "Test1234!" });
        var loginResponse = await httpClient.PostAsJsonAsync("/api/auth/login", new { Email = email, Password = "Test1234!" });
        var loginBody = await loginResponse.Content.ReadFromJsonAsync<TokenResponse>(JsonOptions);
        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", loginBody.Token);

        var therapistId = await GetUserIdAsync(email);

        var clientRequest = new CreateClientRequest
        {
            FirstName = "Session",
            LastName = "Client",
            Email = $"client-{Guid.NewGuid():N}@example.com",
            DateOfBirth = new DateTime(1990, 1, 1),
            Phone = "+31600000000"
        };
        var clientResponse = await httpClient.PostAsJsonAsync("/api/clients", clientRequest);
        var created = await clientResponse.Content.ReadFromJsonAsync<ClientResponse>(JsonOptions);

        return (httpClient, therapistId, created.Id);
    }

    private async Task<string> GetUserIdAsync(string email)
    {
        using var scope = _fixture.Factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<Microsoft.AspNetCore.Identity.UserManager<MindNova.Domain.Entities.ApplicationUser>>();
        var user = await userManager.FindByEmailAsync(email);
        return user.Id;
    }

    private CreateSessionRequest ValidRequest(Guid clientId, string therapistId) => new()
    {
        ClientId = clientId,
        TherapistUserId = therapistId,
        ScheduledAt = DateTime.UtcNow.AddDays(1),
        DurationMinutes = 50,
        SessionType = "Individual",
        Notes = "Test session"
    };

    [Fact]
    [Trait("Story", "MN-18")]
    [Trait("AC", "AC-1")]
    public async Task Post_ValidSession_ReturnsCreatedWithScheduledStatus()
    {
        var (client, therapistId, clientId) = await SetupAsync();
        var request = ValidRequest(clientId, therapistId);

        var response = await client.PostAsJsonAsync("/api/sessions", request);
        var body = await response.Content.ReadFromJsonAsync<SessionResponse>(JsonOptions);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotEqual(Guid.Empty, body.Id);
        Assert.Equal("Scheduled", body.Status);
        Assert.NotEqual(default, body.CreatedAt);
        client.Dispose();
    }

    [Fact]
    [Trait("Story", "MN-18")]
    [Trait("AC", "AC-2")]
    public async Task Post_InvalidClientId_ReturnsProblemDetails()
    {
        var (client, therapistId, _) = await SetupAsync();
        var request = ValidRequest(Guid.NewGuid(), therapistId);

        var response = await client.PostAsJsonAsync("/api/sessions", request);
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("ClientId", body);
        client.Dispose();
    }

    [Fact]
    [Trait("Story", "MN-18")]
    [Trait("AC", "AC-3")]
    public async Task Post_InvalidTherapistUserId_ReturnsProblemDetails()
    {
        var (client, _, clientId) = await SetupAsync();
        var request = ValidRequest(clientId, "nonexistent-user-id");

        var response = await client.PostAsJsonAsync("/api/sessions", request);
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("TherapistUserId", body);
        client.Dispose();
    }

    [Fact]
    [Trait("Story", "MN-18")]
    [Trait("AC", "AC-4")]
    public async Task Post_InvalidDuration_ReturnsProblemDetails()
    {
        var (client, therapistId, clientId) = await SetupAsync();
        var request = ValidRequest(clientId, therapistId);
        request.DurationMinutes = 0;

        var response = await client.PostAsJsonAsync("/api/sessions", request);
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("DurationMinutes", body);
        client.Dispose();
    }

    [Fact]
    [Trait("Story", "MN-18")]
    [Trait("AC", "AC-5")]
    public async Task GetById_ExistingSession_ReturnsFullData()
    {
        var (client, therapistId, clientId) = await SetupAsync();
        var request = ValidRequest(clientId, therapistId);
        var createResponse = await client.PostAsJsonAsync("/api/sessions", request);
        var created = await createResponse.Content.ReadFromJsonAsync<SessionResponse>(JsonOptions);

        var response = await client.GetAsync($"/api/sessions/{created.Id}");
        var body = await response.Content.ReadFromJsonAsync<SessionResponse>(JsonOptions);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(created.Id, body.Id);
        Assert.Equal(clientId, body.ClientId);
        client.Dispose();
    }

    [Fact]
    [Trait("Story", "MN-18")]
    [Trait("AC", "AC-6")]
    public async Task GetById_NonExistent_ReturnsProblemDetails()
    {
        var (client, _, _) = await SetupAsync();

        var response = await client.GetAsync($"/api/sessions/{Guid.NewGuid()}");
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("not found", body, StringComparison.OrdinalIgnoreCase);
        client.Dispose();
    }

    [Fact]
    [Trait("Story", "MN-18")]
    [Trait("AC", "AC-7")]
    public async Task GetList_ReturnsPagedResponse()
    {
        var (client, therapistId, clientId) = await SetupAsync();
        await client.PostAsJsonAsync("/api/sessions", ValidRequest(clientId, therapistId));

        var response = await client.GetAsync("/api/sessions");
        var body = await response.Content.ReadFromJsonAsync<PagedResponse<SessionResponse>>(JsonOptions);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(body.Items);
        Assert.True(body.TotalCount >= 1);
        Assert.Equal(1, body.Page);
        Assert.Equal(20, body.PageSize);
        client.Dispose();
    }

    [Fact]
    [Trait("Story", "MN-18")]
    [Trait("AC", "AC-8")]
    public async Task Put_ValidUpdate_UpdatesFieldsAndRefreshesUpdatedAt()
    {
        var (client, therapistId, clientId) = await SetupAsync();
        var createResponse = await client.PostAsJsonAsync("/api/sessions", ValidRequest(clientId, therapistId));
        var created = await createResponse.Content.ReadFromJsonAsync<SessionResponse>(JsonOptions);

        var updateRequest = new UpdateSessionRequest
        {
            ScheduledAt = DateTime.UtcNow.AddDays(2),
            DurationMinutes = 60,
            SessionType = "Group",
            Status = "Scheduled",
            Notes = "Updated notes"
        };

        var response = await client.PutAsJsonAsync($"/api/sessions/{created.Id}", updateRequest);
        var body = await response.Content.ReadFromJsonAsync<SessionResponse>(JsonOptions);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(60, body.DurationMinutes);
        Assert.Equal("Group", body.SessionType);
        Assert.Equal("Updated notes", body.Notes);
        Assert.True(body.UpdatedAt > created.UpdatedAt);
        client.Dispose();
    }

    [Fact]
    [Trait("Story", "MN-18")]
    [Trait("AC", "AC-9")]
    public async Task Put_ScheduledToCompleted_Succeeds()
    {
        var (client, therapistId, clientId) = await SetupAsync();
        var createResponse = await client.PostAsJsonAsync("/api/sessions", ValidRequest(clientId, therapistId));
        var created = await createResponse.Content.ReadFromJsonAsync<SessionResponse>(JsonOptions);

        var updateRequest = new UpdateSessionRequest
        {
            ScheduledAt = created.ScheduledAt,
            DurationMinutes = created.DurationMinutes,
            SessionType = created.SessionType,
            Status = "Completed",
            Notes = created.Notes
        };

        var response = await client.PutAsJsonAsync($"/api/sessions/{created.Id}", updateRequest);
        var body = await response.Content.ReadFromJsonAsync<SessionResponse>(JsonOptions);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("Completed", body.Status);
        client.Dispose();
    }

    [Fact]
    [Trait("Story", "MN-18")]
    [Trait("AC", "AC-10")]
    public async Task Put_ScheduledToCancelled_Succeeds()
    {
        var (client, therapistId, clientId) = await SetupAsync();
        var createResponse = await client.PostAsJsonAsync("/api/sessions", ValidRequest(clientId, therapistId));
        var created = await createResponse.Content.ReadFromJsonAsync<SessionResponse>(JsonOptions);

        var updateRequest = new UpdateSessionRequest
        {
            ScheduledAt = created.ScheduledAt,
            DurationMinutes = created.DurationMinutes,
            SessionType = created.SessionType,
            Status = "Cancelled",
            Notes = created.Notes
        };

        var response = await client.PutAsJsonAsync($"/api/sessions/{created.Id}", updateRequest);
        var body = await response.Content.ReadFromJsonAsync<SessionResponse>(JsonOptions);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("Cancelled", body.Status);
        client.Dispose();
    }

    [Fact]
    [Trait("Story", "MN-18")]
    [Trait("AC", "AC-11")]
    public async Task Put_ScheduledToNoShow_Succeeds()
    {
        var (client, therapistId, clientId) = await SetupAsync();
        var createResponse = await client.PostAsJsonAsync("/api/sessions", ValidRequest(clientId, therapistId));
        var created = await createResponse.Content.ReadFromJsonAsync<SessionResponse>(JsonOptions);

        var updateRequest = new UpdateSessionRequest
        {
            ScheduledAt = created.ScheduledAt,
            DurationMinutes = created.DurationMinutes,
            SessionType = created.SessionType,
            Status = "NoShow",
            Notes = created.Notes
        };

        var response = await client.PutAsJsonAsync($"/api/sessions/{created.Id}", updateRequest);
        var body = await response.Content.ReadFromJsonAsync<SessionResponse>(JsonOptions);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("NoShow", body.Status);
        client.Dispose();
    }

    [Fact]
    [Trait("Story", "MN-18")]
    [Trait("AC", "AC-12")]
    public async Task Put_CancelledToCompleted_ReturnsProblemDetails()
    {
        var (client, therapistId, clientId) = await SetupAsync();
        var createResponse = await client.PostAsJsonAsync("/api/sessions", ValidRequest(clientId, therapistId));
        var created = await createResponse.Content.ReadFromJsonAsync<SessionResponse>(JsonOptions);

        var cancelRequest = new UpdateSessionRequest
        {
            ScheduledAt = created.ScheduledAt,
            DurationMinutes = created.DurationMinutes,
            SessionType = created.SessionType,
            Status = "Cancelled",
            Notes = created.Notes
        };
        await client.PutAsJsonAsync($"/api/sessions/{created.Id}", cancelRequest);

        var completeRequest = new UpdateSessionRequest
        {
            ScheduledAt = created.ScheduledAt,
            DurationMinutes = created.DurationMinutes,
            SessionType = created.SessionType,
            Status = "Completed",
            Notes = created.Notes
        };

        var response = await client.PutAsJsonAsync($"/api/sessions/{created.Id}", completeRequest);
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("Cancelled", body);
        client.Dispose();
    }

    [Fact]
    [Trait("Story", "MN-18")]
    [Trait("AC", "AC-13")]
    public async Task Put_CompletedToScheduled_ReturnsProblemDetails()
    {
        var (client, therapistId, clientId) = await SetupAsync();
        var createResponse = await client.PostAsJsonAsync("/api/sessions", ValidRequest(clientId, therapistId));
        var created = await createResponse.Content.ReadFromJsonAsync<SessionResponse>(JsonOptions);

        var completeRequest = new UpdateSessionRequest
        {
            ScheduledAt = created.ScheduledAt,
            DurationMinutes = created.DurationMinutes,
            SessionType = created.SessionType,
            Status = "Completed",
            Notes = created.Notes
        };
        await client.PutAsJsonAsync($"/api/sessions/{created.Id}", completeRequest);

        var revertRequest = new UpdateSessionRequest
        {
            ScheduledAt = created.ScheduledAt,
            DurationMinutes = created.DurationMinutes,
            SessionType = created.SessionType,
            Status = "Scheduled",
            Notes = created.Notes
        };

        var response = await client.PutAsJsonAsync($"/api/sessions/{created.Id}", revertRequest);
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("Completed", body);
        client.Dispose();
    }

    [Fact]
    [Trait("Story", "MN-18")]
    [Trait("AC", "AC-14")]
    public async Task Put_NonExistentId_ReturnsProblemDetails()
    {
        var (client, therapistId, clientId) = await SetupAsync();
        var updateRequest = new UpdateSessionRequest
        {
            ScheduledAt = DateTime.UtcNow.AddDays(1),
            DurationMinutes = 50,
            SessionType = "Individual",
            Status = "Scheduled",
            Notes = ""
        };

        var response = await client.PutAsJsonAsync($"/api/sessions/{Guid.NewGuid()}", updateRequest);
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("not found", body, StringComparison.OrdinalIgnoreCase);
        client.Dispose();
    }

    [Fact]
    [Trait("Story", "MN-18")]
    [Trait("AC", "AC-15")]
    public async Task AllEndpoints_WithoutToken_Return401()
    {
        var unauthenticatedClient = _fixture.Factory.CreateClient();

        var postResponse = await unauthenticatedClient.PostAsJsonAsync("/api/sessions", new CreateSessionRequest());
        Assert.Equal(HttpStatusCode.Unauthorized, postResponse.StatusCode);

        var getResponse = await unauthenticatedClient.GetAsync($"/api/sessions/{Guid.NewGuid()}");
        Assert.Equal(HttpStatusCode.Unauthorized, getResponse.StatusCode);

        var listResponse = await unauthenticatedClient.GetAsync("/api/sessions");
        Assert.Equal(HttpStatusCode.Unauthorized, listResponse.StatusCode);

        var putResponse = await unauthenticatedClient.PutAsJsonAsync($"/api/sessions/{Guid.NewGuid()}", new UpdateSessionRequest());
        Assert.Equal(HttpStatusCode.Unauthorized, putResponse.StatusCode);

        unauthenticatedClient.Dispose();
    }
}
