using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using MindNova.Api.Contracts;
using MindNova.Api.Tests.Auth;

namespace MindNova.Api.Tests.Notes;

[Collection("SqlServer")]
public class NoteEndpointTests
{
    private readonly SqlServerFixture _fixture;
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public NoteEndpointTests(SqlServerFixture fixture)
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

    private async Task<(HttpClient Client, string UserId)> RegisterAndLoginAsync(string emailPrefix = "note")
    {
        var httpClient = _fixture.Factory.CreateClient();
        var email = $"{emailPrefix}-{Guid.NewGuid():N}@example.com";
        await httpClient.PostAsJsonAsync("/api/auth/register", new { Email = email, Password = "Test1234!" });
        var loginResponse = await httpClient.PostAsJsonAsync("/api/auth/login", new { Email = email, Password = "Test1234!" });
        var loginBody = await loginResponse.Content.ReadFromJsonAsync<TokenResponse>(JsonOptions);
        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", loginBody.Token);
        var userId = await GetUserIdAsync(email);
        return (httpClient, userId);
    }

    private async Task<Guid> CreateSessionForTherapistAsync(HttpClient client, string therapistUserId)
    {
        var clientRequest = new CreateClientRequest
        {
            FirstName = "Note",
            LastName = "Test",
            Email = $"client-{Guid.NewGuid():N}@example.com",
            DateOfBirth = new DateTime(1990, 1, 1),
            Phone = "+31600000000"
        };
        var clientResponse = await client.PostAsJsonAsync("/api/clients", clientRequest);
        var created = await clientResponse.Content.ReadFromJsonAsync<ClientResponse>(JsonOptions);

        var sessionRequest = new CreateSessionRequest
        {
            ClientId = created.Id,
            TherapistUserId = therapistUserId,
            ScheduledAt = DateTime.UtcNow.AddDays(1),
            DurationMinutes = 50,
            SessionType = "Individual",
            Notes = "Test session for notes"
        };
        var sessionResponse = await client.PostAsJsonAsync("/api/sessions", sessionRequest);
        var session = await sessionResponse.Content.ReadFromJsonAsync<SessionResponse>(JsonOptions);
        return session.Id;
    }

    private CreateNoteRequest ValidNoteRequest() => new()
    {
        PresentingIssue = "Anxiety and stress",
        Interventions = "CBT exposure techniques",
        Homework = "Daily breathing exercises",
        ProgressRating = 7,
        FreeText = "Client showed improvement in coping strategies"
    };

    [Fact]
    [Trait("Story", "MN-33")]
    [Trait("AC", "AC-1")]
    public async Task Post_ValidNote_CreatesAndReturnsNote()
    {
        var (client, userId) = await RegisterAndLoginAsync();
        var sessionId = await CreateSessionForTherapistAsync(client, userId);

        var response = await client.PostAsJsonAsync($"/api/sessions/{sessionId}/notes", ValidNoteRequest());
        var body = await response.Content.ReadFromJsonAsync<TreatmentNoteResponse>(JsonOptions);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotEqual(Guid.Empty, body.Id);
        Assert.Equal(sessionId, body.SessionId);
        Assert.Equal(userId, body.TherapistUserId);
        Assert.Equal("Anxiety and stress", body.PresentingIssue);
        Assert.Equal(7, body.ProgressRating);

        client.Dispose();
    }

    [Fact]
    [Trait("Story", "MN-33")]
    [Trait("AC", "AC-2")]
    public async Task Post_ByNonTherapist_ReturnsForbidden()
    {
        var (therapistClient, therapistId) = await RegisterAndLoginAsync("therapist");
        var sessionId = await CreateSessionForTherapistAsync(therapistClient, therapistId);

        var (otherClient, _) = await RegisterAndLoginAsync("other");

        var response = await otherClient.PostAsJsonAsync($"/api/sessions/{sessionId}/notes", ValidNoteRequest());
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("403", body);

        therapistClient.Dispose();
        otherClient.Dispose();
    }

    [Fact]
    [Trait("Story", "MN-33")]
    [Trait("AC", "AC-3")]
    public async Task Get_ExistingNote_ReturnsNote()
    {
        var (client, userId) = await RegisterAndLoginAsync();
        var sessionId = await CreateSessionForTherapistAsync(client, userId);

        var createResponse = await client.PostAsJsonAsync($"/api/sessions/{sessionId}/notes", ValidNoteRequest());
        var created = await createResponse.Content.ReadFromJsonAsync<TreatmentNoteResponse>(JsonOptions);

        var response = await client.GetAsync($"/api/sessions/{sessionId}/notes/{created.Id}");
        var body = await response.Content.ReadFromJsonAsync<TreatmentNoteResponse>(JsonOptions);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(created.Id, body.Id);
        Assert.Equal("Anxiety and stress", body.PresentingIssue);

        client.Dispose();
    }

    [Fact]
    [Trait("Story", "MN-33")]
    [Trait("AC", "AC-4")]
    public async Task Get_ByNonTherapist_ReturnsForbidden()
    {
        var (therapistClient, therapistId) = await RegisterAndLoginAsync("therapist2");
        var sessionId = await CreateSessionForTherapistAsync(therapistClient, therapistId);
        var createResponse = await therapistClient.PostAsJsonAsync($"/api/sessions/{sessionId}/notes", ValidNoteRequest());
        var created = await createResponse.Content.ReadFromJsonAsync<TreatmentNoteResponse>(JsonOptions);

        var (otherClient, _) = await RegisterAndLoginAsync("other2");

        var response = await otherClient.GetAsync($"/api/sessions/{sessionId}/notes/{created.Id}");
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("403", body);

        therapistClient.Dispose();
        otherClient.Dispose();
    }

    [Fact]
    [Trait("Story", "MN-33")]
    [Trait("AC", "AC-5")]
    public async Task Put_UpdatesContentFields()
    {
        var (client, userId) = await RegisterAndLoginAsync();
        var sessionId = await CreateSessionForTherapistAsync(client, userId);
        var createResponse = await client.PostAsJsonAsync($"/api/sessions/{sessionId}/notes", ValidNoteRequest());
        var created = await createResponse.Content.ReadFromJsonAsync<TreatmentNoteResponse>(JsonOptions);

        var updateRequest = new UpdateNoteRequest
        {
            PresentingIssue = "Updated issue",
            Interventions = "Updated interventions",
            Homework = "Updated homework",
            ProgressRating = 9,
            FreeText = "Updated free text"
        };

        var response = await client.PutAsJsonAsync($"/api/notes/{created.Id}", updateRequest);
        var body = await response.Content.ReadFromJsonAsync<TreatmentNoteResponse>(JsonOptions);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("Updated issue", body.PresentingIssue);
        Assert.Equal(9, body.ProgressRating);
        Assert.True(body.UpdatedAt > created.UpdatedAt);

        client.Dispose();
    }

    [Fact]
    [Trait("Story", "MN-33")]
    [Trait("AC", "AC-6")]
    public async Task AllEndpoints_Unauthenticated_Returns401()
    {
        var unauthClient = _fixture.Factory.CreateClient();
        var fakeId = Guid.NewGuid();

        var postResponse = await unauthClient.PostAsJsonAsync($"/api/sessions/{fakeId}/notes", ValidNoteRequest());
        var getResponse = await unauthClient.GetAsync($"/api/sessions/{fakeId}/notes/{fakeId}");
        var listResponse = await unauthClient.GetAsync($"/api/sessions/{fakeId}/notes");
        var putResponse = await unauthClient.PutAsJsonAsync($"/api/notes/{fakeId}", new UpdateNoteRequest());

        Assert.Equal(HttpStatusCode.Unauthorized, postResponse.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, getResponse.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, listResponse.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, putResponse.StatusCode);

        unauthClient.Dispose();
    }

    [Fact]
    [Trait("Story", "MN-33")]
    [Trait("AC", "AC-7")]
    public async Task Post_InvalidProgressRating_ReturnsProblemDetails()
    {
        var (client, userId) = await RegisterAndLoginAsync();
        var sessionId = await CreateSessionForTherapistAsync(client, userId);

        var request = ValidNoteRequest();
        request.ProgressRating = 15;

        var response = await client.PostAsJsonAsync($"/api/sessions/{sessionId}/notes", request);
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("ProgressRating", body);

        client.Dispose();
    }

    [Fact]
    [Trait("Story", "MN-33")]
    [Trait("AC", "AC-7")]
    public async Task Post_InvalidSessionId_ReturnsProblemDetails()
    {
        var (client, _) = await RegisterAndLoginAsync();

        var response = await client.PostAsJsonAsync($"/api/sessions/{Guid.NewGuid()}/notes", ValidNoteRequest());
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("not found", body, StringComparison.OrdinalIgnoreCase);

        client.Dispose();
    }
}
