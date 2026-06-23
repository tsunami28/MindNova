using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
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

    private async Task<string> GetUserIdAsync(string email)
    {
        using var scope = _fixture.Factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<Microsoft.AspNetCore.Identity.UserManager<MindNova.Domain.Entities.ApplicationUser>>();
        var user = await userManager.FindByEmailAsync(email);
        return user.Id;
    }

    private async Task<(HttpClient Client, string TherapistUserId)> SetupAsync()
    {
        var email = $"avail-{Guid.NewGuid():N}@example.com";
        await _fixture.Client.PostAsJsonAsync("/api/auth/register", new { Email = email, Password = "Test1234!" });
        var loginResponse = await _fixture.Client.PostAsJsonAsync("/api/auth/login", new { Email = email, Password = "Test1234!" });
        var loginBody = await loginResponse.Content.ReadFromJsonAsync<TokenResponse>(JsonOptions);

        var client = _fixture.Factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", loginBody.Token);

        var therapistId = await GetUserIdAsync(email);
        return (client, therapistId);
    }

    private static CreateAvailabilityBlockRequest RecurringRequest(string therapistId) => new()
    {
        TherapistUserId = therapistId,
        DayOfWeek = 1, // Monday
        StartTime = "09:00:00",
        EndTime = "17:00:00",
        EffectiveFrom = DateTime.UtcNow,
        IsRecurring = true
    };

    private static CreateAvailabilityBlockRequest OneOffRequest(string therapistId) => new()
    {
        TherapistUserId = therapistId,
        StartTime = "10:00:00",
        EndTime = "14:00:00",
        EffectiveFrom = DateTime.UtcNow,
        IsRecurring = false,
        SpecificDate = DateTime.UtcNow.AddDays(7)
    };

    [Fact]
    [Trait("Story", "MN-21")]
    [Trait("AC", "AC-1")]
    public async Task Post_RecurringBlock_ReturnsCreatedWithGeneratedFields()
    {
        var (client, therapistId) = await SetupAsync();
        var request = RecurringRequest(therapistId);

        var response = await client.PostAsJsonAsync("/api/availability", request);
        var body = await response.Content.ReadFromJsonAsync<AvailabilityBlockResponse>(JsonOptions);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotEqual(Guid.Empty, body.Id);
        Assert.True(body.IsRecurring);
        Assert.Equal(1, body.DayOfWeek);
        Assert.NotEqual(default, body.CreatedAt);
    }

    [Fact]
    [Trait("Story", "MN-21")]
    [Trait("AC", "AC-2")]
    public async Task Post_OneOffBlock_ReturnsCreated()
    {
        var (client, therapistId) = await SetupAsync();
        var request = OneOffRequest(therapistId);

        var response = await client.PostAsJsonAsync("/api/availability", request);
        var body = await response.Content.ReadFromJsonAsync<AvailabilityBlockResponse>(JsonOptions);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.False(body.IsRecurring);
        Assert.NotNull(body.SpecificDate);
    }

    [Fact]
    [Trait("Story", "MN-21")]
    [Trait("AC", "AC-3")]
    public async Task Post_InvalidTherapistUserId_ReturnsProblemDetails()
    {
        var (client, _) = await SetupAsync();
        var request = RecurringRequest("nonexistent-user-id");

        var response = await client.PostAsJsonAsync("/api/availability", request);
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("TherapistUserId", body);
    }

    [Fact]
    [Trait("Story", "MN-21")]
    [Trait("AC", "AC-4")]
    public async Task Post_InvalidData_ReturnsProblemDetails()
    {
        var (client, therapistId) = await SetupAsync();
        var request = RecurringRequest(therapistId);
        request.StartTime = "17:00:00";
        request.EndTime = "09:00:00"; // StartTime >= EndTime

        var response = await client.PostAsJsonAsync("/api/availability", request);
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("StartTime", body);
    }

    [Fact]
    [Trait("Story", "MN-21")]
    [Trait("AC", "AC-5")]
    public async Task GetList_ReturnsBlocksForTherapist()
    {
        var (client, therapistId) = await SetupAsync();
        await client.PostAsJsonAsync("/api/availability", RecurringRequest(therapistId));

        var response = await client.GetAsync($"/api/availability?therapist_id={therapistId}");
        var body = await response.Content.ReadFromJsonAsync<List<AvailabilityBlockResponse>>(JsonOptions);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotEmpty(body);
        Assert.All(body, b => Assert.Equal(therapistId, b.TherapistUserId));
    }

    [Fact]
    [Trait("Story", "MN-21")]
    [Trait("AC", "AC-6")]
    public async Task GetList_NoBlocks_ReturnsEmptyList()
    {
        var (client, therapistId) = await SetupAsync();

        var response = await client.GetAsync($"/api/availability?therapist_id={therapistId}");
        var body = await response.Content.ReadFromJsonAsync<List<AvailabilityBlockResponse>>(JsonOptions);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Empty(body);
    }

    [Fact]
    [Trait("Story", "MN-21")]
    [Trait("AC", "AC-7")]
    public async Task GetById_ExistingBlock_ReturnsFullData()
    {
        var (client, therapistId) = await SetupAsync();
        var createResponse = await client.PostAsJsonAsync("/api/availability", RecurringRequest(therapistId));
        var created = await createResponse.Content.ReadFromJsonAsync<AvailabilityBlockResponse>(JsonOptions);

        var response = await client.GetAsync($"/api/availability/{created.Id}");
        var body = await response.Content.ReadFromJsonAsync<AvailabilityBlockResponse>(JsonOptions);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(created.Id, body.Id);
    }

    [Fact]
    [Trait("Story", "MN-21")]
    [Trait("AC", "AC-8")]
    public async Task GetById_NonExistent_ReturnsProblemDetails()
    {
        var (client, _) = await SetupAsync();

        var response = await client.GetAsync($"/api/availability/{Guid.NewGuid()}");
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("not found", body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    [Trait("Story", "MN-21")]
    [Trait("AC", "AC-9")]
    public async Task Put_ValidData_UpdatesAndRefreshesUpdatedAt()
    {
        var (client, therapistId) = await SetupAsync();
        var createResponse = await client.PostAsJsonAsync("/api/availability", RecurringRequest(therapistId));
        var created = await createResponse.Content.ReadFromJsonAsync<AvailabilityBlockResponse>(JsonOptions);

        var updateRequest = new UpdateAvailabilityBlockRequest
        {
            DayOfWeek = 2, // Tuesday
            StartTime = "10:00:00",
            EndTime = "16:00:00",
            EffectiveFrom = DateTime.UtcNow,
            IsRecurring = true
        };

        var response = await client.PutAsJsonAsync($"/api/availability/{created.Id}", updateRequest);
        var body = await response.Content.ReadFromJsonAsync<AvailabilityBlockResponse>(JsonOptions);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(2, body.DayOfWeek);
        Assert.Equal("10:00:00", body.StartTime);
        Assert.True(body.UpdatedAt > created.UpdatedAt);
    }

    [Fact]
    [Trait("Story", "MN-21")]
    [Trait("AC", "AC-10")]
    public async Task Put_NonExistent_ReturnsProblemDetails()
    {
        var (client, _) = await SetupAsync();
        var updateRequest = new UpdateAvailabilityBlockRequest
        {
            DayOfWeek = 1,
            StartTime = "09:00:00",
            EndTime = "17:00:00",
            EffectiveFrom = DateTime.UtcNow,
            IsRecurring = true
        };

        var response = await client.PutAsJsonAsync($"/api/availability/{Guid.NewGuid()}", updateRequest);
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("not found", body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    [Trait("Story", "MN-21")]
    [Trait("AC", "AC-11")]
    public async Task Delete_ExistingBlock_ReturnsConfirmation()
    {
        var (client, therapistId) = await SetupAsync();
        var createResponse = await client.PostAsJsonAsync("/api/availability", RecurringRequest(therapistId));
        var created = await createResponse.Content.ReadFromJsonAsync<AvailabilityBlockResponse>(JsonOptions);

        var response = await client.DeleteAsync($"/api/availability/{created.Id}");
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("true", body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    [Trait("Story", "MN-21")]
    [Trait("AC", "AC-12")]
    public async Task Delete_NonExistent_ReturnsProblemDetails()
    {
        var (client, _) = await SetupAsync();

        var response = await client.DeleteAsync($"/api/availability/{Guid.NewGuid()}");
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("not found", body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    [Trait("Story", "MN-21")]
    [Trait("AC", "AC-13")]
    public async Task AllEndpoints_WithoutToken_Return401()
    {
        var unauthenticatedClient = _fixture.Factory.CreateClient();

        var postResponse = await unauthenticatedClient.PostAsJsonAsync("/api/availability", new CreateAvailabilityBlockRequest());
        Assert.Equal(HttpStatusCode.Unauthorized, postResponse.StatusCode);

        var getResponse = await unauthenticatedClient.GetAsync("/api/availability?therapist_id=x");
        Assert.Equal(HttpStatusCode.Unauthorized, getResponse.StatusCode);

        var getByIdResponse = await unauthenticatedClient.GetAsync($"/api/availability/{Guid.NewGuid()}");
        Assert.Equal(HttpStatusCode.Unauthorized, getByIdResponse.StatusCode);

        var putResponse = await unauthenticatedClient.PutAsJsonAsync($"/api/availability/{Guid.NewGuid()}", new UpdateAvailabilityBlockRequest());
        Assert.Equal(HttpStatusCode.Unauthorized, putResponse.StatusCode);

        var deleteResponse = await unauthenticatedClient.DeleteAsync($"/api/availability/{Guid.NewGuid()}");
        Assert.Equal(HttpStatusCode.Unauthorized, deleteResponse.StatusCode);
    }
}
