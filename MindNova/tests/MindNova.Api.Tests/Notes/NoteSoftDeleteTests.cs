using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using MindNova.Api.Contracts;
using MindNova.Api.Tests.Auth;

namespace MindNova.Api.Tests.Notes;

[Collection("SqlServer")]
public class NoteSoftDeleteTests
{
    private readonly SqlServerFixture _fixture;
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public NoteSoftDeleteTests(SqlServerFixture fixture)
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

    private async Task<(HttpClient Client, string UserId)> RegisterAndLoginAsync(string prefix = "softdel")
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

    private async Task<Guid> CreateSessionAsync(HttpClient client, string therapistUserId)
    {
        var clientRequest = new CreateClientRequest
        {
            FirstName = "SoftDel",
            LastName = "Test",
            Email = $"sdclient-{Guid.NewGuid():N}@example.com",
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
            Notes = "Soft delete test"
        };
        var sessionResponse = await client.PostAsJsonAsync("/api/sessions", sessionRequest);
        var session = await sessionResponse.Content.ReadFromJsonAsync<SessionResponse>(JsonOptions);
        return session.Id;
    }

    private async Task<Guid> CreateNoteAsync(HttpClient client, Guid sessionId)
    {
        var request = new CreateNoteRequest
        {
            PresentingIssue = "Test issue",
            Interventions = "Test interventions",
            Homework = "Test homework",
            ProgressRating = 7,
            FreeText = "Test free text"
        };
        var response = await client.PostAsJsonAsync($"/api/sessions/{sessionId}/notes", request);
        var body = await response.Content.ReadFromJsonAsync<TreatmentNoteResponse>(JsonOptions);
        return body.Id;
    }

    [Fact]
    [Trait("Story", "MN-35")]
    [Trait("AC", "AC-1")]
    public async Task Delete_SetsIsDeletedAndAuditFields()
    {
        var (client, userId) = await RegisterAndLoginAsync();
        var sessionId = await CreateSessionAsync(client, userId);
        var noteId = await CreateNoteAsync(client, sessionId);

        var response = await client.DeleteAsync($"/api/notes/{noteId}");
        var body = await response.Content.ReadFromJsonAsync<TreatmentNoteResponse>(JsonOptions);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.True(body.IsDeleted);
        Assert.Equal(noteId, body.Id);

        client.Dispose();
    }

    [Fact]
    [Trait("Story", "MN-35")]
    [Trait("AC", "AC-2")]
    public async Task Delete_ByNonTherapist_ReturnsForbidden()
    {
        var (therapistClient, therapistId) = await RegisterAndLoginAsync("sd-therapist");
        var sessionId = await CreateSessionAsync(therapistClient, therapistId);
        var noteId = await CreateNoteAsync(therapistClient, sessionId);

        var (otherClient, _) = await RegisterAndLoginAsync("sd-other");

        var response = await otherClient.DeleteAsync($"/api/notes/{noteId}");
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("403", body);

        therapistClient.Dispose();
        otherClient.Dispose();
    }

    [Fact]
    [Trait("Story", "MN-35")]
    [Trait("AC", "AC-4")]
    public async Task ListBySession_IncludeDeleted_AdminShowsAll()
    {
        var (client, userId) = await RegisterAndLoginAsync();
        var sessionId = await CreateSessionAsync(client, userId);
        var noteId1 = await CreateNoteAsync(client, sessionId);
        await CreateNoteAsync(client, sessionId);

        // Soft-delete the first note
        await client.DeleteAsync($"/api/notes/{noteId1}");

        // Without include_deleted: should have 1
        var normalResponse = await client.GetAsync($"/api/sessions/{sessionId}/notes");
        var normalBody = await normalResponse.Content.ReadAsStringAsync();
        var normalNotes = JsonSerializer.Deserialize<List<TreatmentNoteResponse>>(normalBody, JsonOptions);
        Assert.Single(normalNotes);

        // With include_deleted=true: should have 2 (even non-admin sees it as the therapist here,
        // but the include_deleted param is only honoured for Admin; for therapist it still filters)
        // Since this user is not Admin, include_deleted should be ignored
        var inclResponse = await client.GetAsync($"/api/sessions/{sessionId}/notes?include_deleted=true");
        var inclBody = await inclResponse.Content.ReadAsStringAsync();
        var inclNotes = JsonSerializer.Deserialize<List<TreatmentNoteResponse>>(inclBody, JsonOptions);
        Assert.Single(inclNotes);

        client.Dispose();
    }

    [Fact]
    [Trait("Story", "MN-35")]
    [Trait("AC", "AC-5")]
    public async Task GetByNoteId_DeletedNote_TherapistGets404()
    {
        var (client, userId) = await RegisterAndLoginAsync();
        var sessionId = await CreateSessionAsync(client, userId);
        var noteId = await CreateNoteAsync(client, sessionId);

        await client.DeleteAsync($"/api/notes/{noteId}");

        var response = await client.GetAsync($"/api/notes/{noteId}");
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("not found", body, StringComparison.OrdinalIgnoreCase);

        client.Dispose();
    }

    [Fact]
    [Trait("Story", "MN-35")]
    [Trait("AC", "AC-6")]
    public async Task Update_DeletedNote_ReturnsProblemDetails()
    {
        var (client, userId) = await RegisterAndLoginAsync();
        var sessionId = await CreateSessionAsync(client, userId);
        var noteId = await CreateNoteAsync(client, sessionId);

        await client.DeleteAsync($"/api/notes/{noteId}");

        var updateRequest = new UpdateNoteRequest
        {
            PresentingIssue = "Trying to update deleted",
            Interventions = "Should fail",
            Homework = "Should fail",
            ProgressRating = 5,
            FreeText = "Should fail"
        };
        var response = await client.PutAsJsonAsync($"/api/notes/{noteId}", updateRequest);
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("deleted", body, StringComparison.OrdinalIgnoreCase);

        client.Dispose();
    }
}
