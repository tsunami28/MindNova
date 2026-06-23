using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using MindNova.Api.Contracts;
using MindNova.Api.Tests.Auth;

namespace MindNova.Api.Tests.Sessions;

[Collection("SqlServer")]
public class ConflictDetectionTests
{
    private readonly SqlServerFixture _fixture;
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public ConflictDetectionTests(SqlServerFixture fixture)
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

    private async Task<(HttpClient Client, string TherapistUserId, Guid ClientId)> SetupAsync()
    {
        var email = $"conflict-{Guid.NewGuid():N}@example.com";
        await _fixture.Client.PostAsJsonAsync("/api/auth/register", new { Email = email, Password = "Test1234!" });
        var loginResponse = await _fixture.Client.PostAsJsonAsync("/api/auth/login", new { Email = email, Password = "Test1234!" });
        var loginBody = await loginResponse.Content.ReadFromJsonAsync<TokenResponse>(JsonOptions);

        var client = _fixture.Factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", loginBody.Token);

        var therapistId = await GetUserIdAsync(email);

        var clientRequest = new CreateClientRequest { FirstName = "Conflict", LastName = "Test", Email = $"cc-{Guid.NewGuid():N}@example.com" };
        var clientResponse = await client.PostAsJsonAsync("/api/clients", clientRequest);
        var createdClient = await clientResponse.Content.ReadFromJsonAsync<ClientResponse>(JsonOptions);

        return (client, therapistId, createdClient.Id);
    }

    private async Task CreateRecurringAvailabilityAsync(HttpClient client, string therapistId, DayOfWeek day, string startTime, string endTime)
    {
        await client.PostAsJsonAsync("/api/availability", new CreateAvailabilityBlockRequest
        {
            TherapistUserId = therapistId,
            DayOfWeek = (int)day,
            StartTime = startTime,
            EndTime = endTime,
            EffectiveFrom = DateTime.UtcNow.AddDays(-30),
            IsRecurring = true
        });
    }

    private async Task CreateOneOffAvailabilityAsync(HttpClient client, string therapistId, DateTime date, string startTime, string endTime)
    {
        await client.PostAsJsonAsync("/api/availability", new CreateAvailabilityBlockRequest
        {
            TherapistUserId = therapistId,
            StartTime = startTime,
            EndTime = endTime,
            EffectiveFrom = DateTime.UtcNow.AddDays(-30),
            IsRecurring = false,
            SpecificDate = date
        });
    }

    private DateTime NextDayOfWeek(DayOfWeek day)
    {
        var today = DateTime.UtcNow.Date;
        var daysUntil = ((int)day - (int)today.DayOfWeek + 7) % 7;
        if (daysUntil == 0) daysUntil = 7;
        return today.AddDays(daysUntil);
    }

    [Fact]
    [Trait("Story", "MN-22")]
    [Trait("AC", "AC-1")]
    public async Task Create_FullOverlap_ReturnsProblemDetails()
    {
        var (client, therapistId, clientId) = await SetupAsync();
        var monday = NextDayOfWeek(DayOfWeek.Monday);
        await CreateRecurringAvailabilityAsync(client, therapistId, DayOfWeek.Monday, "08:00:00", "18:00:00");

        await client.PostAsJsonAsync("/api/sessions", new CreateSessionRequest
        {
            ClientId = clientId, TherapistUserId = therapistId,
            ScheduledAt = monday.AddHours(10), DurationMinutes = 50, SessionType = "Individual"
        });

        var response = await client.PostAsJsonAsync("/api/sessions", new CreateSessionRequest
        {
            ClientId = clientId, TherapistUserId = therapistId,
            ScheduledAt = monday.AddHours(10), DurationMinutes = 50, SessionType = "Individual"
        });
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("overlap", body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    [Trait("Story", "MN-22")]
    [Trait("AC", "AC-2")]
    public async Task Create_PartialOverlap_ReturnsProblemDetails()
    {
        var (client, therapistId, clientId) = await SetupAsync();
        var monday = NextDayOfWeek(DayOfWeek.Monday);
        await CreateRecurringAvailabilityAsync(client, therapistId, DayOfWeek.Monday, "08:00:00", "18:00:00");

        await client.PostAsJsonAsync("/api/sessions", new CreateSessionRequest
        {
            ClientId = clientId, TherapistUserId = therapistId,
            ScheduledAt = monday.AddHours(10), DurationMinutes = 60, SessionType = "Individual"
        });

        var response = await client.PostAsJsonAsync("/api/sessions", new CreateSessionRequest
        {
            ClientId = clientId, TherapistUserId = therapistId,
            ScheduledAt = monday.AddHours(10).AddMinutes(30), DurationMinutes = 60, SessionType = "Individual"
        });
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("overlap", body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    [Trait("Story", "MN-22")]
    [Trait("AC", "AC-3")]
    public async Task Create_OutsideAvailability_ReturnsProblemDetails()
    {
        var (client, therapistId, clientId) = await SetupAsync();
        var tuesday = NextDayOfWeek(DayOfWeek.Tuesday);
        // No availability on Tuesday

        var response = await client.PostAsJsonAsync("/api/sessions", new CreateSessionRequest
        {
            ClientId = clientId, TherapistUserId = therapistId,
            ScheduledAt = tuesday.AddHours(10), DurationMinutes = 50, SessionType = "Individual"
        });
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("availability", body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    [Trait("Story", "MN-22")]
    [Trait("AC", "AC-4")]
    public async Task Create_WithinRecurringAvailability_Succeeds()
    {
        var (client, therapistId, clientId) = await SetupAsync();
        var wednesday = NextDayOfWeek(DayOfWeek.Wednesday);
        await CreateRecurringAvailabilityAsync(client, therapistId, DayOfWeek.Wednesday, "09:00:00", "17:00:00");

        var response = await client.PostAsJsonAsync("/api/sessions", new CreateSessionRequest
        {
            ClientId = clientId, TherapistUserId = therapistId,
            ScheduledAt = wednesday.AddHours(10), DurationMinutes = 50, SessionType = "Individual"
        });
        var body = await response.Content.ReadFromJsonAsync<SessionResponse>(JsonOptions);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotEqual(Guid.Empty, body.Id);
    }

    [Fact]
    [Trait("Story", "MN-22")]
    [Trait("AC", "AC-5")]
    public async Task Create_WithinOneOffAvailability_Succeeds()
    {
        var (client, therapistId, clientId) = await SetupAsync();
        var saturday = NextDayOfWeek(DayOfWeek.Saturday);
        await CreateOneOffAvailabilityAsync(client, therapistId, saturday, "10:00:00", "14:00:00");

        var response = await client.PostAsJsonAsync("/api/sessions", new CreateSessionRequest
        {
            ClientId = clientId, TherapistUserId = therapistId,
            ScheduledAt = saturday.AddHours(10), DurationMinutes = 50, SessionType = "Individual"
        });
        var body = await response.Content.ReadFromJsonAsync<SessionResponse>(JsonOptions);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotEqual(Guid.Empty, body.Id);
    }

    [Fact]
    [Trait("Story", "MN-22")]
    [Trait("AC", "AC-6")]
    public async Task Update_ToOverlappingTime_ReturnsProblemDetails()
    {
        var (client, therapistId, clientId) = await SetupAsync();
        var thursday = NextDayOfWeek(DayOfWeek.Thursday);
        await CreateRecurringAvailabilityAsync(client, therapistId, DayOfWeek.Thursday, "08:00:00", "18:00:00");

        var s1Response = await client.PostAsJsonAsync("/api/sessions", new CreateSessionRequest
        {
            ClientId = clientId, TherapistUserId = therapistId,
            ScheduledAt = thursday.AddHours(10), DurationMinutes = 60, SessionType = "Individual"
        });
        var s1 = await s1Response.Content.ReadFromJsonAsync<SessionResponse>(JsonOptions);

        var s2Response = await client.PostAsJsonAsync("/api/sessions", new CreateSessionRequest
        {
            ClientId = clientId, TherapistUserId = therapistId,
            ScheduledAt = thursday.AddHours(14), DurationMinutes = 60, SessionType = "Individual"
        });
        var s2 = await s2Response.Content.ReadFromJsonAsync<SessionResponse>(JsonOptions);

        var response = await client.PutAsJsonAsync($"/api/sessions/{s2.Id}", new UpdateSessionRequest
        {
            ScheduledAt = thursday.AddHours(10), DurationMinutes = 60,
            SessionType = "Individual", Notes = ""
        });
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("overlap", body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    [Trait("Story", "MN-22")]
    [Trait("AC", "AC-7")]
    public async Task Update_OutsideAvailability_ReturnsProblemDetails()
    {
        var (client, therapistId, clientId) = await SetupAsync();
        var friday = NextDayOfWeek(DayOfWeek.Friday);
        await CreateRecurringAvailabilityAsync(client, therapistId, DayOfWeek.Friday, "09:00:00", "12:00:00");

        var createResponse = await client.PostAsJsonAsync("/api/sessions", new CreateSessionRequest
        {
            ClientId = clientId, TherapistUserId = therapistId,
            ScheduledAt = friday.AddHours(9), DurationMinutes = 50, SessionType = "Individual"
        });
        var created = await createResponse.Content.ReadFromJsonAsync<SessionResponse>(JsonOptions);

        var response = await client.PutAsJsonAsync($"/api/sessions/{created.Id}", new UpdateSessionRequest
        {
            ScheduledAt = friday.AddHours(14), DurationMinutes = 50,
            SessionType = "Individual", Notes = ""
        });
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("availability", body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    [Trait("Story", "MN-22")]
    [Trait("AC", "AC-8")]
    public async Task Update_ToValidSlot_Succeeds()
    {
        var (client, therapistId, clientId) = await SetupAsync();
        var monday = NextDayOfWeek(DayOfWeek.Monday);
        await CreateRecurringAvailabilityAsync(client, therapistId, DayOfWeek.Monday, "08:00:00", "18:00:00");

        var createResponse = await client.PostAsJsonAsync("/api/sessions", new CreateSessionRequest
        {
            ClientId = clientId, TherapistUserId = therapistId,
            ScheduledAt = monday.AddHours(10), DurationMinutes = 50, SessionType = "Individual"
        });
        var created = await createResponse.Content.ReadFromJsonAsync<SessionResponse>(JsonOptions);

        var response = await client.PutAsJsonAsync($"/api/sessions/{created.Id}", new UpdateSessionRequest
        {
            ScheduledAt = monday.AddHours(14), DurationMinutes = 50,
            SessionType = "Individual", Notes = "Moved"
        });
        var body = await response.Content.ReadFromJsonAsync<SessionResponse>(JsonOptions);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("Moved", body.Notes);
    }

    [Fact]
    [Trait("Story", "MN-22")]
    [Trait("AC", "AC-9")]
    public async Task Create_ExactFillOfAvailability_Succeeds()
    {
        var (client, therapistId, clientId) = await SetupAsync();
        var wednesday = NextDayOfWeek(DayOfWeek.Wednesday);
        await CreateRecurringAvailabilityAsync(client, therapistId, DayOfWeek.Wednesday, "10:00:00", "11:00:00");

        var response = await client.PostAsJsonAsync("/api/sessions", new CreateSessionRequest
        {
            ClientId = clientId, TherapistUserId = therapistId,
            ScheduledAt = wednesday.AddHours(10), DurationMinutes = 60, SessionType = "Individual"
        });
        var body = await response.Content.ReadFromJsonAsync<SessionResponse>(JsonOptions);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotEqual(Guid.Empty, body.Id);
    }

    [Fact]
    [Trait("Story", "MN-22")]
    [Trait("AC", "AC-10")]
    public async Task Create_DifferentTherapists_SameTime_NoConflict()
    {
        var (client1, therapistId1, clientId1) = await SetupAsync();
        var (client2, therapistId2, clientId2) = await SetupAsync();
        var monday = NextDayOfWeek(DayOfWeek.Monday);

        await CreateRecurringAvailabilityAsync(client1, therapistId1, DayOfWeek.Monday, "08:00:00", "18:00:00");
        await CreateRecurringAvailabilityAsync(client2, therapistId2, DayOfWeek.Monday, "08:00:00", "18:00:00");

        var r1 = await client1.PostAsJsonAsync("/api/sessions", new CreateSessionRequest
        {
            ClientId = clientId1, TherapistUserId = therapistId1,
            ScheduledAt = monday.AddHours(10), DurationMinutes = 50, SessionType = "Individual"
        });
        var r2 = await client2.PostAsJsonAsync("/api/sessions", new CreateSessionRequest
        {
            ClientId = clientId2, TherapistUserId = therapistId2,
            ScheduledAt = monday.AddHours(10), DurationMinutes = 50, SessionType = "Individual"
        });

        var b1 = await r1.Content.ReadFromJsonAsync<SessionResponse>(JsonOptions);
        var b2 = await r2.Content.ReadFromJsonAsync<SessionResponse>(JsonOptions);

        Assert.NotEqual(Guid.Empty, b1.Id);
        Assert.NotEqual(Guid.Empty, b2.Id);
    }
}
