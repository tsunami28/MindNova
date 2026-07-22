using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using MindNova.Api.Contracts;
using MindNova.Api.Tests.Auth;

namespace MindNova.Api.Tests.Calendar;

[Collection("SqlServer")]
public class CalendarEndpointTests
{
    private readonly SqlServerFixture _fixture;
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public CalendarEndpointTests(SqlServerFixture fixture)
    {
        _fixture = fixture;
    }

    private class TokenResponse
    {
        public string Token { get; set; } = string.Empty;
    }

    private async Task<(HttpClient Client, Guid TherapistProfileId, string TherapistUserId)> SetupWithTherapistAsync()
    {
        var httpClient = _fixture.Factory.CreateClient();
        var email = $"cal-{Guid.NewGuid():N}@example.com";
        await httpClient.PostAsJsonAsync("/api/auth/register", new { Email = email, Password = "Test1234!" });
        var loginResponse = await httpClient.PostAsJsonAsync("/api/auth/login", new { Email = email, Password = "Test1234!" });
        var loginBody = await loginResponse.Content.ReadFromJsonAsync<TokenResponse>(JsonOptions);
        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", loginBody.Token);

        using var scope = _fixture.Factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<Microsoft.AspNetCore.Identity.UserManager<MindNova.Domain.Entities.ApplicationUser>>();
        var user = await userManager.FindByEmailAsync(email);

        var profileRequest = new CreateTherapistRequest
        {
            UserId = user.Id,
            Specialisations = new List<string> { "CBT" },
            MaxCaseload = 10
        };
        var profileResponse = await httpClient.PostAsJsonAsync("/api/therapists", profileRequest);
        var profile = await profileResponse.Content.ReadFromJsonAsync<TherapistProfileResponse>(JsonOptions);

        return (httpClient, profile.Id, user.Id);
    }

    private async Task CreateAvailabilitySlotAsync(HttpClient client, Guid profileId, int dayOfWeek)
    {
        var request = new CreateAvailabilityRequest
        {
            DayOfWeek = dayOfWeek,
            SpecificDate = null,
            StartTime = new TimeSpan(9, 0, 0),
            EndTime = new TimeSpan(17, 0, 0),
            IsRecurring = true
        };
        var response = await client.PostAsJsonAsync($"/api/therapists/{profileId}/availability", request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    private async Task CreateOneOffSlotAsync(HttpClient client, Guid profileId, DateTime date, TimeSpan start, TimeSpan end)
    {
        var request = new CreateAvailabilityRequest
        {
            DayOfWeek = null,
            SpecificDate = date.Date,
            StartTime = start,
            EndTime = end,
            IsRecurring = false
        };
        var response = await client.PostAsJsonAsync($"/api/therapists/{profileId}/availability", request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
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
            Notes = "Calendar test"
        };
        var response = await client.PostAsJsonAsync("/api/sessions", request);
        var body = await response.Content.ReadFromJsonAsync<SessionResponse>(JsonOptions);
        return body.Id;
    }

    private async Task<Guid> CreateClientAsync(HttpClient client)
    {
        var clientRequest = new CreateClientRequest
        {
            FirstName = "Calendar",
            LastName = "Test",
            Email = $"client-{Guid.NewGuid():N}@example.com",
            DateOfBirth = new DateTime(1990, 1, 1),
            Phone = "+31600000000"
        };
        var clientResponse = await client.PostAsJsonAsync("/api/clients", clientRequest);
        var created = await clientResponse.Content.ReadFromJsonAsync<ClientResponse>(JsonOptions);
        return created.Id;
    }

    [Fact]
    [Trait("Story", "MN-31")]
    [Trait("AC", "AC-1")]
    public async Task Get_WithDateRange_ReturnsCalendarEntryList()
    {
        var (client, profileId, _) = await SetupWithTherapistAsync();

        var dateFrom = DateTime.UtcNow.Date.AddDays(20);
        var dateTo = dateFrom.AddDays(6);
        await CreateOneOffSlotAsync(client, profileId, dateFrom, new TimeSpan(9, 0, 0), new TimeSpan(12, 0, 0));

        var response = await client.GetAsync($"/api/therapists/{profileId}/calendar?date_from={dateFrom:yyyy-MM-dd}&date_to={dateTo:yyyy-MM-dd}");
        var body = await response.Content.ReadFromJsonAsync<CalendarResponse>(JsonOptions);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(body.Items);
        Assert.True(body.Items.Count >= 1);

        client.Dispose();
    }

    [Fact]
    [Trait("Story", "MN-31")]
    [Trait("AC", "AC-2")]
    public async Task Get_EntryHasRequiredFields()
    {
        var (client, profileId, _) = await SetupWithTherapistAsync();

        var dateFrom = DateTime.UtcNow.Date.AddDays(21);
        var dateTo = dateFrom;
        await CreateOneOffSlotAsync(client, profileId, dateFrom, new TimeSpan(10, 0, 0), new TimeSpan(14, 0, 0));

        var response = await client.GetAsync($"/api/therapists/{profileId}/calendar?date_from={dateFrom:yyyy-MM-dd}&date_to={dateTo:yyyy-MM-dd}");
        var body = await response.Content.ReadFromJsonAsync<CalendarResponse>(JsonOptions);

        var entry = Assert.Single(body.Items);
        Assert.Equal(dateFrom.Date, entry.Date.Date);
        Assert.Equal(new TimeSpan(10, 0, 0), entry.StartTime);
        Assert.Equal(new TimeSpan(14, 0, 0), entry.EndTime);
        Assert.Equal("Availability", entry.EntryType);
        Assert.NotEqual(Guid.Empty, entry.SourceId);

        client.Dispose();
    }

    [Fact]
    [Trait("Story", "MN-31")]
    [Trait("AC", "AC-3")]
    public async Task Get_RecurringSlotExpandedIntoConcreteDates()
    {
        var (client, profileId, _) = await SetupWithTherapistAsync();

        // Create a recurring Monday slot
        await CreateAvailabilitySlotAsync(client, profileId, dayOfWeek: 1);

        // Query a 14-day range that should contain at least 2 Mondays
        var today = DateTime.UtcNow.Date;
        var nextMonday = today.AddDays(((int)System.DayOfWeek.Monday - (int)today.DayOfWeek + 7) % 7 + 28);
        var dateFrom = nextMonday;
        var dateTo = nextMonday.AddDays(13);

        var response = await client.GetAsync($"/api/therapists/{profileId}/calendar?date_from={dateFrom:yyyy-MM-dd}&date_to={dateTo:yyyy-MM-dd}");
        var body = await response.Content.ReadFromJsonAsync<CalendarResponse>(JsonOptions);

        var availEntries = body.Items.Where(e => e.EntryType == "Availability").ToList();
        Assert.True(availEntries.Count >= 2, $"Expected at least 2 expanded Monday entries, got {availEntries.Count}");
        Assert.All(availEntries, e => Assert.Equal(System.DayOfWeek.Monday, e.Date.DayOfWeek));

        client.Dispose();
    }

    [Fact]
    [Trait("Story", "MN-31")]
    [Trait("AC", "AC-4")]
    public async Task Get_SessionsAppearAsSessionTypeEntries()
    {
        var (client, profileId, therapistUserId) = await SetupWithTherapistAsync();
        var clientId = await CreateClientAsync(client);

        var sessionDate = DateTime.UtcNow.Date.AddDays(22);
        await CreateOneOffSlotAsync(client, profileId, sessionDate, new TimeSpan(8, 0, 0), new TimeSpan(18, 0, 0));
        var sessionId = await CreateSessionAsync(client, clientId, therapistUserId, sessionDate.AddHours(10));

        var response = await client.GetAsync($"/api/therapists/{profileId}/calendar?date_from={sessionDate:yyyy-MM-dd}&date_to={sessionDate:yyyy-MM-dd}");
        var body = await response.Content.ReadFromJsonAsync<CalendarResponse>(JsonOptions);

        var sessionEntries = body.Items.Where(e => e.EntryType == "Session").ToList();
        Assert.Single(sessionEntries);
        Assert.Equal(sessionId, sessionEntries[0].SourceId);

        client.Dispose();
    }

    [Fact]
    [Trait("Story", "MN-31")]
    [Trait("AC", "AC-5")]
    public async Task Get_EmptyRange_ReturnsEmptyList()
    {
        var (client, profileId, _) = await SetupWithTherapistAsync();

        // Query a range with no slots or sessions
        var dateFrom = DateTime.UtcNow.Date.AddDays(60);
        var dateTo = dateFrom.AddDays(1);

        var response = await client.GetAsync($"/api/therapists/{profileId}/calendar?date_from={dateFrom:yyyy-MM-dd}&date_to={dateTo:yyyy-MM-dd}");
        var body = await response.Content.ReadFromJsonAsync<CalendarResponse>(JsonOptions);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(body.Items);
        Assert.Empty(body.Items);

        client.Dispose();
    }

    [Fact]
    [Trait("Story", "MN-31")]
    [Trait("AC", "AC-6")]
    public async Task Get_Unauthenticated_Returns401()
    {
        var unauthClient = _fixture.Factory.CreateClient();

        var response = await unauthClient.GetAsync($"/api/therapists/{Guid.NewGuid()}/calendar?date_from=2026-08-01&date_to=2026-08-07");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);

        unauthClient.Dispose();
    }
}
