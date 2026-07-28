using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using MindNova.Api.Contracts;
using MindNova.Api.Tests.Auth;

namespace MindNova.Api.Tests.Notes;

[Collection("SqlServer")]
public class NoteQueryTests
{
    private readonly SqlServerFixture _fixture;
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public NoteQueryTests(SqlServerFixture fixture)
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

    private async Task<(HttpClient Client, string UserId)> RegisterAndLoginAsync(string prefix = "noteq")
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
            FirstName = "NoteQuery",
            LastName = "Test",
            Email = $"nqclient-{Guid.NewGuid():N}@example.com",
            DateOfBirth = new DateTime(1990, 1, 1),
            Phone = "+31600000000"
        };
        var response = await client.PostAsJsonAsync("/api/clients", request);
        var body = await response.Content.ReadFromJsonAsync<ClientResponse>(JsonOptions);
        return body.Id;
    }

    private async Task<Guid> CreateSessionAsync(HttpClient client, Guid clientId, string therapistUserId, DateTime scheduledAt)
    {
        var request = new CreateSessionRequest
        {
            ClientId = clientId,
            TherapistUserId = therapistUserId,
            ScheduledAt = scheduledAt,
            DurationMinutes = 50,
            SessionType = "Individual",
            Notes = "Query test session"
        };
        var response = await client.PostAsJsonAsync("/api/sessions", request);
        var body = await response.Content.ReadFromJsonAsync<SessionResponse>(JsonOptions);
        return body.Id;
    }

    private async Task CreateNoteAsync(HttpClient client, Guid sessionId, int rating = 7)
    {
        var request = new CreateNoteRequest
        {
            PresentingIssue = "Test issue",
            Interventions = "Test interventions",
            Homework = "Test homework",
            ProgressRating = rating,
            FreeText = "Test free text"
        };
        await client.PostAsJsonAsync($"/api/sessions/{sessionId}/notes", request);
    }

    [Fact]
    [Trait("Story", "MN-34")]
    [Trait("AC", "AC-1")]
    public async Task Get_ClientNotes_ReturnsNotesAcrossSessions()
    {
        var (client, userId) = await RegisterAndLoginAsync();
        var clientId = await CreateClientAsync(client);

        var session1Id = await CreateSessionAsync(client, clientId, userId, DateTime.UtcNow.AddDays(-5));
        var session2Id = await CreateSessionAsync(client, clientId, userId, DateTime.UtcNow.AddDays(-2));
        await CreateNoteAsync(client, session1Id, 5);
        await CreateNoteAsync(client, session2Id, 8);

        var response = await client.GetAsync($"/api/clients/{clientId}/notes");
        var body = await response.Content.ReadFromJsonAsync<PagedResponse<TreatmentNoteResponse>>(JsonOptions);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(2, body.TotalCount);
        Assert.Equal(2, body.Items.Count);
        // Sorted by session date descending: session2 first
        Assert.Equal(session2Id, body.Items[0].SessionId);
        Assert.Equal(session1Id, body.Items[1].SessionId);

        client.Dispose();
    }

    [Fact]
    [Trait("Story", "MN-34")]
    [Trait("AC", "AC-2")]
    public async Task Get_ClientNotes_WithDateFilter_FiltersCorrectly()
    {
        var (client, userId) = await RegisterAndLoginAsync();
        var clientId = await CreateClientAsync(client);

        var oldDate = DateTime.UtcNow.AddDays(-30);
        var recentDate = DateTime.UtcNow.AddDays(-1);
        var oldSessionId = await CreateSessionAsync(client, clientId, userId, oldDate);
        var recentSessionId = await CreateSessionAsync(client, clientId, userId, recentDate);
        await CreateNoteAsync(client, oldSessionId, 4);
        await CreateNoteAsync(client, recentSessionId, 9);

        var dateFrom = DateTime.UtcNow.AddDays(-5).ToString("yyyy-MM-dd");
        var response = await client.GetAsync($"/api/clients/{clientId}/notes?date_from={dateFrom}");
        var body = await response.Content.ReadFromJsonAsync<PagedResponse<TreatmentNoteResponse>>(JsonOptions);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(1, body.TotalCount);
        Assert.Equal(recentSessionId, body.Items[0].SessionId);

        client.Dispose();
    }

    [Fact]
    [Trait("Story", "MN-34")]
    [Trait("AC", "AC-3")]
    public async Task Get_ClientNotes_SupportsPagination()
    {
        var (client, userId) = await RegisterAndLoginAsync();
        var clientId = await CreateClientAsync(client);

        // Create 3 sessions with notes
        for (int i = 0; i < 3; i++)
        {
            var sessionId = await CreateSessionAsync(client, clientId, userId, DateTime.UtcNow.AddDays(-10 + i));
            await CreateNoteAsync(client, sessionId, 5 + i);
        }

        var response = await client.GetAsync($"/api/clients/{clientId}/notes?page=1&page_size=2");
        var body = await response.Content.ReadFromJsonAsync<PagedResponse<TreatmentNoteResponse>>(JsonOptions);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(3, body.TotalCount);
        Assert.Equal(2, body.Items.Count);
        Assert.Equal(1, body.Page);
        Assert.Equal(2, body.PageSize);

        client.Dispose();
    }

    [Fact]
    [Trait("Story", "MN-34")]
    [Trait("AC", "AC-4")]
    public async Task Get_ClientNotes_ByNonTherapist_ReturnsForbidden()
    {
        var (therapistClient, therapistId) = await RegisterAndLoginAsync("therapist-q");
        var clientId = await CreateClientAsync(therapistClient);
        var sessionId = await CreateSessionAsync(therapistClient, clientId, therapistId, DateTime.UtcNow.AddDays(-1));
        await CreateNoteAsync(therapistClient, sessionId);

        var (otherClient, _) = await RegisterAndLoginAsync("other-q");

        var response = await otherClient.GetAsync($"/api/clients/{clientId}/notes");
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("403", body);

        therapistClient.Dispose();
        otherClient.Dispose();
    }

    [Fact]
    [Trait("Story", "MN-34")]
    [Trait("AC", "AC-5")]
    public async Task Get_ClientNotes_ExcludesDeletedByDefault()
    {
        var (client, userId) = await RegisterAndLoginAsync();
        var clientId = await CreateClientAsync(client);
        var sessionId = await CreateSessionAsync(client, clientId, userId, DateTime.UtcNow.AddDays(-1));
        await CreateNoteAsync(client, sessionId, 6);
        await CreateNoteAsync(client, sessionId, 7);

        // Soft-delete one note via the DB directly
        using (var scope = _fixture.Factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<MindNova.Infrastructure.Data.MindNovaDbContext>();
            var note = db.TreatmentNotes.First(n => n.SessionId == sessionId && n.ProgressRating == 6);
            note.IsDeleted = true;
            note.DeletedAt = DateTime.UtcNow;
            note.DeletedByUserId = userId;
            await db.SaveChangesAsync();
        }

        var response = await client.GetAsync($"/api/clients/{clientId}/notes");
        var body = await response.Content.ReadFromJsonAsync<PagedResponse<TreatmentNoteResponse>>(JsonOptions);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(1, body.TotalCount);
        Assert.Equal(7, body.Items[0].ProgressRating);

        client.Dispose();
    }

    [Fact]
    [Trait("Story", "MN-34")]
    [Trait("AC", "AC-6")]
    public async Task Get_ClientNotes_Unauthenticated_Returns401()
    {
        var unauthClient = _fixture.Factory.CreateClient();

        var response = await unauthClient.GetAsync($"/api/clients/{Guid.NewGuid()}/notes");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);

        unauthClient.Dispose();
    }
}
