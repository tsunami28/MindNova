using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using MindNova.Api.Contracts;
using MindNova.Api.Tests.Auth;

namespace MindNova.Api.Tests.Sessions;

[Collection("SqlServer")]
public class SessionConflictTests
{
    private readonly SqlServerFixture _fixture;
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public SessionConflictTests(SqlServerFixture fixture)
    {
        _fixture = fixture;
    }

    private class TokenResponse
    {
        public string Token { get; set; } = string.Empty;
    }

    private async Task<(HttpClient Client, string TherapistUserId, Guid ClientId, Guid TherapistProfileId)> SetupAsync()
    {
        var httpClient = _fixture.Factory.CreateClient();

        var email = $"conflict-{Guid.NewGuid():N}@example.com";
        await httpClient.PostAsJsonAsync("/api/auth/register", new { Email = email, Password = "Test1234!" });
        var loginResponse = await httpClient.PostAsJsonAsync("/api/auth/login", new { Email = email, Password = "Test1234!" });
        var loginBody = await loginResponse.Content.ReadFromJsonAsync<TokenResponse>(JsonOptions);
        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", loginBody.Token);

        using var scope = _fixture.Factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<Microsoft.AspNetCore.Identity.UserManager<MindNova.Domain.Entities.ApplicationUser>>();
        var user = await userManager.FindByEmailAsync(email);
        var therapistUserId = user.Id;

        var profileRequest = new CreateTherapistRequest
        {
            UserId = therapistUserId,
            Specialisations = new List<string> { "CBT" },
            MaxCaseload = 10
        };
        var profileResponse = await httpClient.PostAsJsonAsync("/api/therapists", profileRequest);
        var profile = await profileResponse.Content.ReadFromJsonAsync<TherapistProfileResponse>(JsonOptions);

        var clientRequest = new CreateClientRequest
        {
            FirstName = "Conflict",
            LastName = "Test",
            Email = $"client-{Guid.NewGuid():N}@example.com",
            DateOfBirth = new DateTime(1990, 1, 1),
            Phone = "+31600000000"
        };
        var clientResponse = await httpClient.PostAsJsonAsync("/api/clients", clientRequest);
        var created = await clientResponse.Content.ReadFromJsonAsync<ClientResponse>(JsonOptions);

        return (httpClient, therapistUserId, created.Id, profile.Id);
    }

    private async Task CreateAvailabilitySlotAsync(HttpClient client, Guid therapistProfileId, DateTime date, TimeSpan startTime, TimeSpan endTime)
    {
        var request = new CreateAvailabilityRequest
        {
            DayOfWeek = null,
            SpecificDate = date.Date,
            StartTime = startTime,
            EndTime = endTime,
            IsRecurring = false
        };
        var response = await client.PostAsJsonAsync($"/api/therapists/{therapistProfileId}/availability", request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    private CreateSessionRequest ValidRequest(Guid clientId, string therapistId, DateTime scheduledAt, int durationMinutes = 50) => new()
    {
        ClientId = clientId,
        TherapistUserId = therapistId,
        ScheduledAt = scheduledAt,
        DurationMinutes = durationMinutes,
        SessionType = "Individual",
        Notes = "Test session"
    };

    [Fact]
    [Trait("Story", "MN-30")]
    [Trait("AC", "AC-1")]
    public async Task Post_OverlappingSession_ReturnsConflictError()
    {
        var (client, therapistId, clientId, profileId) = await SetupAsync();

        // Create an availability slot covering the whole day
        var sessionDate = DateTime.UtcNow.Date.AddDays(10);
        await CreateAvailabilitySlotAsync(client, profileId, sessionDate, new TimeSpan(8, 0, 0), new TimeSpan(18, 0, 0));

        // First session: 10:00-10:50
        var firstSession = ValidRequest(clientId, therapistId, sessionDate.AddHours(10), 50);
        var firstResponse = await client.PostAsJsonAsync("/api/sessions", firstSession);
        Assert.Equal(HttpStatusCode.OK, firstResponse.StatusCode);
        var firstBody = await firstResponse.Content.ReadAsStringAsync();
        Assert.DoesNotContain("urn:mindnova:error", firstBody);

        // Second session: 10:30-11:20 (overlaps first)
        var conflictSession = ValidRequest(clientId, therapistId, sessionDate.AddHours(10).AddMinutes(30), 50);
        var conflictResponse = await client.PostAsJsonAsync("/api/sessions", conflictSession);
        var conflictBody = await conflictResponse.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, conflictResponse.StatusCode);
        Assert.Contains("urn:mindnova:error:session-conflict", conflictBody);

        client.Dispose();
    }

    [Fact]
    [Trait("Story", "MN-30")]
    [Trait("AC", "AC-2")]
    public async Task Post_OverlappingSession_ReturnsProblemDetailsWithConflictDetail()
    {
        var (client, therapistId, clientId, profileId) = await SetupAsync();

        var sessionDate = DateTime.UtcNow.Date.AddDays(11);
        await CreateAvailabilitySlotAsync(client, profileId, sessionDate, new TimeSpan(8, 0, 0), new TimeSpan(18, 0, 0));

        // First session at 14:00
        var firstSession = ValidRequest(clientId, therapistId, sessionDate.AddHours(14), 60);
        await client.PostAsJsonAsync("/api/sessions", firstSession);

        // Overlapping session at 14:30
        var conflictSession = ValidRequest(clientId, therapistId, sessionDate.AddHours(14).AddMinutes(30), 60);
        var conflictResponse = await client.PostAsJsonAsync("/api/sessions", conflictSession);
        var conflictBody = await conflictResponse.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, conflictResponse.StatusCode);
        Assert.Contains("Session conflict", conflictBody);
        Assert.Contains("conflictingSessionId", conflictBody);
        Assert.Contains("conflictingStart", conflictBody);
        Assert.Contains("conflictingEnd", conflictBody);

        client.Dispose();
    }

    [Fact]
    [Trait("Story", "MN-30")]
    [Trait("AC", "AC-3")]
    public async Task Post_OutsideAvailability_ReturnsOutsideAvailabilityError()
    {
        var (client, therapistId, clientId, profileId) = await SetupAsync();

        // Create a narrow availability slot: 9:00-12:00
        var sessionDate = DateTime.UtcNow.Date.AddDays(12);
        await CreateAvailabilitySlotAsync(client, profileId, sessionDate, new TimeSpan(9, 0, 0), new TimeSpan(12, 0, 0));

        // Session at 14:00 - outside availability
        var session = ValidRequest(clientId, therapistId, sessionDate.AddHours(14), 50);
        var response = await client.PostAsJsonAsync("/api/sessions", session);
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("urn:mindnova:error:outside-availability", body);

        client.Dispose();
    }

    [Fact]
    [Trait("Story", "MN-30")]
    [Trait("AC", "AC-4")]
    public async Task Post_WithinAvailabilityAndNoConflict_Succeeds()
    {
        var (client, therapistId, clientId, profileId) = await SetupAsync();

        // Create availability slot: 9:00-17:00
        var sessionDate = DateTime.UtcNow.Date.AddDays(13);
        await CreateAvailabilitySlotAsync(client, profileId, sessionDate, new TimeSpan(9, 0, 0), new TimeSpan(17, 0, 0));

        // Session at 10:00 within availability
        var session = ValidRequest(clientId, therapistId, sessionDate.AddHours(10), 50);
        var response = await client.PostAsJsonAsync("/api/sessions", session);
        var body = await response.Content.ReadFromJsonAsync<SessionResponse>(JsonOptions);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotEqual(Guid.Empty, body.Id);
        Assert.Equal("Scheduled", body.Status);

        client.Dispose();
    }

    [Fact]
    [Trait("Story", "MN-30")]
    [Trait("AC", "AC-5")]
    public async Task Put_UpdateToOverlappingTime_ReturnsConflictError()
    {
        var (client, therapistId, clientId, profileId) = await SetupAsync();

        // Create availability slot: 8:00-18:00
        var sessionDate = DateTime.UtcNow.Date.AddDays(14);
        await CreateAvailabilitySlotAsync(client, profileId, sessionDate, new TimeSpan(8, 0, 0), new TimeSpan(18, 0, 0));

        // First session at 10:00
        var firstSession = ValidRequest(clientId, therapistId, sessionDate.AddHours(10), 60);
        await client.PostAsJsonAsync("/api/sessions", firstSession);

        // Second session at 15:00 (no conflict)
        var secondSession = ValidRequest(clientId, therapistId, sessionDate.AddHours(15), 60);
        var secondResponse = await client.PostAsJsonAsync("/api/sessions", secondSession);
        var created = await secondResponse.Content.ReadFromJsonAsync<SessionResponse>(JsonOptions);

        // Update second session to 10:30 (now overlaps first)
        var updateRequest = new UpdateSessionRequest
        {
            ScheduledAt = sessionDate.AddHours(10).AddMinutes(30),
            DurationMinutes = 60,
            SessionType = "Individual",
            Notes = "Updated"
        };
        var updateResponse = await client.PutAsJsonAsync($"/api/sessions/{created.Id}", updateRequest);
        var updateBody = await updateResponse.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);
        Assert.Contains("urn:mindnova:error:session-conflict", updateBody);

        client.Dispose();
    }

    [Fact]
    [Trait("Story", "MN-30")]
    [Trait("AC", "AC-6")]
    public async Task Post_ValidSession_StillWorksWithoutRegression()
    {
        var (client, therapistId, clientId, profileId) = await SetupAsync();

        // Create availability for the session
        var sessionDate = DateTime.UtcNow.Date.AddDays(15);
        await CreateAvailabilitySlotAsync(client, profileId, sessionDate, new TimeSpan(8, 0, 0), new TimeSpan(18, 0, 0));

        var request = ValidRequest(clientId, therapistId, sessionDate.AddHours(9), 50);
        var response = await client.PostAsJsonAsync("/api/sessions", request);
        var body = await response.Content.ReadFromJsonAsync<SessionResponse>(JsonOptions);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotEqual(Guid.Empty, body.Id);
        Assert.Equal("Scheduled", body.Status);
        Assert.Equal(clientId, body.ClientId);
        Assert.Equal(therapistId, body.TherapistUserId);

        client.Dispose();
    }
}
