using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using MindNova.Api.Contracts;
using MindNova.Api.Tests.Auth;

namespace MindNova.Api.Tests.Availability;

[Collection("SqlServer")]
public class AvailabilityEndpointTests
{
    private readonly SqlServerFixture _fixture;
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public AvailabilityEndpointTests(SqlServerFixture fixture)
    {
        _fixture = fixture;
    }

    private class TokenResponse
    {
        public string Token { get; set; } = string.Empty;
    }

    private async Task<(HttpClient Client, Guid TherapistProfileId)> SetupWithTherapistAsync()
    {
        var httpClient = _fixture.Factory.CreateClient();
        var email = $"avail-{Guid.NewGuid():N}@example.com";
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

        return (httpClient, profile.Id);
    }

    private CreateAvailabilityRequest RecurringRequest(int dayOfWeek = 1) => new()
    {
        DayOfWeek = dayOfWeek,
        SpecificDate = null,
        StartTime = new TimeSpan(9, 0, 0),
        EndTime = new TimeSpan(12, 0, 0),
        IsRecurring = true
    };

    [Fact]
    [Trait("Story", "MN-29")]
    [Trait("AC", "AC-1")]
    public async Task Post_ValidSlot_ReturnsCreated()
    {
        var (client, profileId) = await SetupWithTherapistAsync();

        var response = await client.PostAsJsonAsync($"/api/therapists/{profileId}/availability", RecurringRequest());
        var body = await response.Content.ReadFromJsonAsync<AvailabilitySlotResponse>(JsonOptions);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotEqual(Guid.Empty, body.Id);
        Assert.Equal(profileId, body.TherapistProfileId);
        Assert.True(body.IsRecurring);
        Assert.Equal(1, body.DayOfWeek);
    }

    [Fact]
    [Trait("Story", "MN-29")]
    [Trait("AC", "AC-2")]
    public async Task Post_InvalidTherapistId_ReturnsProblemDetails()
    {
        var (client, _) = await SetupWithTherapistAsync();

        var response = await client.PostAsJsonAsync($"/api/therapists/{Guid.NewGuid()}/availability", RecurringRequest());
        var body = await response.Content.ReadFromJsonAsync<ProblemDetails>(JsonOptions);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(404, body.Status);
    }

    [Fact]
    [Trait("Story", "MN-29")]
    [Trait("AC", "AC-3")]
    public async Task Post_OverlappingSlot_ReturnsProblemDetails()
    {
        var (client, profileId) = await SetupWithTherapistAsync();

        await client.PostAsJsonAsync($"/api/therapists/{profileId}/availability", RecurringRequest());

        var overlapping = new CreateAvailabilityRequest
        {
            DayOfWeek = 1,
            StartTime = new TimeSpan(10, 0, 0),
            EndTime = new TimeSpan(13, 0, 0),
            IsRecurring = true
        };
        var response = await client.PostAsJsonAsync($"/api/therapists/{profileId}/availability", overlapping);
        var body = await response.Content.ReadFromJsonAsync<ProblemDetails>(JsonOptions);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(400, body.Status);
        Assert.Contains("overlap", body.Detail, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    [Trait("Story", "MN-29")]
    [Trait("AC", "AC-4")]
    public async Task Get_ReturnsAllSlotsForTherapist()
    {
        var (client, profileId) = await SetupWithTherapistAsync();

        await client.PostAsJsonAsync($"/api/therapists/{profileId}/availability", RecurringRequest(1));
        await client.PostAsJsonAsync($"/api/therapists/{profileId}/availability", RecurringRequest(3));

        var response = await client.GetAsync($"/api/therapists/{profileId}/availability");
        var body = await response.Content.ReadFromJsonAsync<List<AvailabilitySlotResponse>>(JsonOptions);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(2, body.Count);
    }

    [Fact]
    [Trait("Story", "MN-29")]
    [Trait("AC", "AC-5")]
    public async Task Get_WithDateFilter_FiltersOneOffSlots()
    {
        var (client, profileId) = await SetupWithTherapistAsync();

        var oneOff = new CreateAvailabilityRequest
        {
            SpecificDate = new DateTime(2026, 8, 15),
            StartTime = new TimeSpan(14, 0, 0),
            EndTime = new TimeSpan(17, 0, 0),
            IsRecurring = false
        };
        await client.PostAsJsonAsync($"/api/therapists/{profileId}/availability", oneOff);

        var response = await client.GetAsync($"/api/therapists/{profileId}/availability?date_from=2026-08-01&date_to=2026-08-31");
        var body = await response.Content.ReadFromJsonAsync<List<AvailabilitySlotResponse>>(JsonOptions);

        Assert.NotEmpty(body);
        Assert.Contains(body, s => s.SpecificDate.HasValue && s.SpecificDate.Value.Month == 8);
    }

    [Fact]
    [Trait("Story", "MN-29")]
    [Trait("AC", "AC-6")]
    public async Task Put_UpdatesSlotTimes()
    {
        var (client, profileId) = await SetupWithTherapistAsync();

        var createResponse = await client.PostAsJsonAsync($"/api/therapists/{profileId}/availability", RecurringRequest());
        var created = await createResponse.Content.ReadFromJsonAsync<AvailabilitySlotResponse>(JsonOptions);

        var updateRequest = new UpdateAvailabilityRequest
        {
            StartTime = new TimeSpan(10, 0, 0),
            EndTime = new TimeSpan(14, 0, 0)
        };
        var response = await client.PutAsJsonAsync($"/api/availability/{created.Id}", updateRequest);
        var body = await response.Content.ReadFromJsonAsync<AvailabilitySlotResponse>(JsonOptions);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(new TimeSpan(10, 0, 0), body.StartTime);
        Assert.Equal(new TimeSpan(14, 0, 0), body.EndTime);
    }

    [Fact]
    [Trait("Story", "MN-29")]
    [Trait("AC", "AC-7")]
    public async Task Delete_RemovesSlot()
    {
        var (client, profileId) = await SetupWithTherapistAsync();

        var createResponse = await client.PostAsJsonAsync($"/api/therapists/{profileId}/availability", RecurringRequest());
        var created = await createResponse.Content.ReadFromJsonAsync<AvailabilitySlotResponse>(JsonOptions);

        var response = await client.DeleteAsync($"/api/availability/{created.Id}");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var listResponse = await client.GetAsync($"/api/therapists/{profileId}/availability");
        var list = await listResponse.Content.ReadFromJsonAsync<List<AvailabilitySlotResponse>>(JsonOptions);

        Assert.DoesNotContain(list, s => s.Id == created.Id);
    }

    [Fact]
    [Trait("Story", "MN-29")]
    [Trait("AC", "AC-8")]
    public async Task AllEndpoints_Unauthenticated_Returns401()
    {
        var unauthClient = _fixture.Factory.CreateClient();
        var fakeId = Guid.NewGuid();

        var postResponse = await unauthClient.PostAsJsonAsync($"/api/therapists/{fakeId}/availability", RecurringRequest());
        var getResponse = await unauthClient.GetAsync($"/api/therapists/{fakeId}/availability");
        var putResponse = await unauthClient.PutAsJsonAsync($"/api/availability/{fakeId}", new UpdateAvailabilityRequest());
        var deleteResponse = await unauthClient.DeleteAsync($"/api/availability/{fakeId}");

        Assert.Equal(HttpStatusCode.Unauthorized, postResponse.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, getResponse.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, putResponse.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, deleteResponse.StatusCode);
    }
}
