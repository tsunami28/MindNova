using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using MindNova.Api.Contracts;
using MindNova.Api.Tests.Auth;

namespace MindNova.Api.Tests.Therapists;

[Collection("SqlServer")]
public class CaseloadEndpointTests
{
    private readonly SqlServerFixture _fixture;
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public CaseloadEndpointTests(SqlServerFixture fixture)
    {
        _fixture = fixture;
    }

    private class TokenResponse
    {
        public string Token { get; set; } = string.Empty;
    }

    private async Task<(HttpClient Client, string UserId)> SetupAuthenticatedAsync()
    {
        var httpClient = _fixture.Factory.CreateClient();
        var email = $"caseload-{Guid.NewGuid():N}@example.com";
        await httpClient.PostAsJsonAsync("/api/auth/register", new { Email = email, Password = "Test1234!" });
        var loginResponse = await httpClient.PostAsJsonAsync("/api/auth/login", new { Email = email, Password = "Test1234!" });
        var loginBody = await loginResponse.Content.ReadFromJsonAsync<TokenResponse>(JsonOptions);
        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", loginBody.Token);

        var userId = await GetUserIdAsync(email);
        return (httpClient, userId);
    }

    private async Task<string> GetUserIdAsync(string email)
    {
        using var scope = _fixture.Factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<Microsoft.AspNetCore.Identity.UserManager<MindNova.Domain.Entities.ApplicationUser>>();
        var user = await userManager.FindByEmailAsync(email);
        return user.Id;
    }

    private async Task<Guid> CreateTherapistProfileAsync(HttpClient client, string userId, int maxCaseload = 10)
    {
        var request = new CreateTherapistRequest
        {
            UserId = userId,
            Specialisations = new List<string> { "CBT" },
            MaxCaseload = maxCaseload
        };
        var response = await client.PostAsJsonAsync("/api/therapists", request);
        var body = await response.Content.ReadFromJsonAsync<TherapistProfileResponse>(JsonOptions);
        return body.Id;
    }

    private async Task<Guid> CreateClientAsync(HttpClient client)
    {
        var request = new CreateClientRequest
        {
            FirstName = "Caseload",
            LastName = $"Test-{Guid.NewGuid():N}",
            Email = $"cl-{Guid.NewGuid():N}@example.com",
            DateOfBirth = new DateTime(1988, 6, 15),
            Phone = "+31600000003"
        };
        var response = await client.PostAsJsonAsync("/api/clients", request);
        var body = await response.Content.ReadFromJsonAsync<ClientResponse>(JsonOptions);
        return body.Id;
    }

    [Fact]
    [Trait("Story", "MN-27")]
    [Trait("AC", "AC-1")]
    public async Task GetCaseload_ReturnsList()
    {
        var (client, userId) = await SetupAuthenticatedAsync();
        await CreateTherapistProfileAsync(client, userId);

        var response = await client.GetAsync("/api/therapists/caseload");
        var body = await response.Content.ReadFromJsonAsync<List<CaseloadSummaryResponse>>(JsonOptions);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(body);
        Assert.True(body.Count >= 1);
    }

    [Fact]
    [Trait("Story", "MN-27")]
    [Trait("AC", "AC-2")]
    public async Task GetCaseload_IncludesAllRequiredFields()
    {
        var (client, userId) = await SetupAuthenticatedAsync();
        var profileId = await CreateTherapistProfileAsync(client, userId, maxCaseload: 8);

        var response = await client.GetAsync("/api/therapists/caseload");
        var body = await response.Content.ReadFromJsonAsync<List<CaseloadSummaryResponse>>(JsonOptions);

        var summary = body.First(s => s.TherapistProfileId == profileId);
        Assert.NotEqual(Guid.Empty, summary.TherapistProfileId);
        Assert.False(string.IsNullOrEmpty(summary.TherapistName));
        Assert.Equal(8, summary.MaxCaseload);
        Assert.True(summary.CurrentCaseload >= 0);
        Assert.True(summary.AvailableCapacity >= 0);
    }

    [Fact]
    [Trait("Story", "MN-27")]
    [Trait("AC", "AC-3")]
    public async Task GetCaseload_CurrentCaseload_CountsAssignedClients()
    {
        var (client, userId) = await SetupAuthenticatedAsync();
        var profileId = await CreateTherapistProfileAsync(client, userId, maxCaseload: 5);

        var clientId1 = await CreateClientAsync(client);
        var clientId2 = await CreateClientAsync(client);
        await client.PostAsJsonAsync($"/api/clients/{clientId1}/therapist",
            new AssignTherapistRequest { TherapistProfileId = profileId });
        await client.PostAsJsonAsync($"/api/clients/{clientId2}/therapist",
            new AssignTherapistRequest { TherapistProfileId = profileId });

        var response = await client.GetAsync("/api/therapists/caseload");
        var body = await response.Content.ReadFromJsonAsync<List<CaseloadSummaryResponse>>(JsonOptions);

        var summary = body.First(s => s.TherapistProfileId == profileId);
        Assert.Equal(2, summary.CurrentCaseload);
    }

    [Fact]
    [Trait("Story", "MN-27")]
    [Trait("AC", "AC-4")]
    public async Task GetCaseload_AvailableCapacity_EqualsMaxMinusCurrent()
    {
        var (client, userId) = await SetupAuthenticatedAsync();
        var profileId = await CreateTherapistProfileAsync(client, userId, maxCaseload: 5);

        var clientId = await CreateClientAsync(client);
        await client.PostAsJsonAsync($"/api/clients/{clientId}/therapist",
            new AssignTherapistRequest { TherapistProfileId = profileId });

        var response = await client.GetAsync("/api/therapists/caseload");
        var body = await response.Content.ReadFromJsonAsync<List<CaseloadSummaryResponse>>(JsonOptions);

        var summary = body.First(s => s.TherapistProfileId == profileId);
        Assert.Equal(5 - 1, summary.AvailableCapacity);
        Assert.Equal(summary.MaxCaseload - summary.CurrentCaseload, summary.AvailableCapacity);
    }

    [Fact]
    [Trait("Story", "MN-27")]
    [Trait("AC", "AC-5")]
    public async Task GetCaseload_ExcludesInactiveTherapists()
    {
        var (client, userId) = await SetupAuthenticatedAsync();
        var profileId = await CreateTherapistProfileAsync(client, userId);

        await client.DeleteAsync($"/api/therapists/{profileId}");

        var response = await client.GetAsync("/api/therapists/caseload");
        var body = await response.Content.ReadFromJsonAsync<List<CaseloadSummaryResponse>>(JsonOptions);

        Assert.DoesNotContain(body, s => s.TherapistProfileId == profileId);
    }

    [Fact]
    [Trait("Story", "MN-27")]
    [Trait("AC", "AC-6")]
    public async Task GetCaseload_Unauthenticated_Returns401()
    {
        var unauthClient = _fixture.Factory.CreateClient();

        var response = await unauthClient.GetAsync("/api/therapists/caseload");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
