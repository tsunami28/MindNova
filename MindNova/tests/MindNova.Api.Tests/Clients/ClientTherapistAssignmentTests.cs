using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using MindNova.Api.Contracts;
using MindNova.Api.Tests.Auth;

namespace MindNova.Api.Tests.Clients;

[Collection("SqlServer")]
public class ClientTherapistAssignmentTests
{
    private readonly SqlServerFixture _fixture;
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public ClientTherapistAssignmentTests(SqlServerFixture fixture)
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
        var email = $"assign-{Guid.NewGuid():N}@example.com";
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

    private async Task<Guid> CreateClientAsync(HttpClient client)
    {
        var request = new CreateClientRequest
        {
            FirstName = "Assign",
            LastName = $"Test-{Guid.NewGuid():N}",
            Email = $"assign-client-{Guid.NewGuid():N}@example.com",
            DateOfBirth = new DateTime(1990, 5, 10),
            Phone = "+31600000002"
        };
        var response = await client.PostAsJsonAsync("/api/clients", request);
        var body = await response.Content.ReadFromJsonAsync<ClientResponse>(JsonOptions);
        return body.Id;
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

    [Fact]
    [Trait("Story", "MN-26")]
    [Trait("AC", "AC-1")]
    public async Task AssignTherapist_ValidRequest_ReturnsUpdatedClient()
    {
        var (client, userId) = await SetupAuthenticatedAsync();
        var clientId = await CreateClientAsync(client);
        var profileId = await CreateTherapistProfileAsync(client, userId);

        var response = await client.PostAsJsonAsync($"/api/clients/{clientId}/therapist",
            new AssignTherapistRequest { TherapistProfileId = profileId });
        var body = await response.Content.ReadFromJsonAsync<ClientResponse>(JsonOptions);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(profileId, body.AssignedTherapistId);
    }

    [Fact]
    [Trait("Story", "MN-26")]
    [Trait("AC", "AC-2")]
    public async Task AssignTherapist_CaseloadExceeded_ReturnsProblemDetails()
    {
        var (client, userId) = await SetupAuthenticatedAsync();
        var profileId = await CreateTherapistProfileAsync(client, userId, maxCaseload: 1);

        var clientId1 = await CreateClientAsync(client);
        await client.PostAsJsonAsync($"/api/clients/{clientId1}/therapist",
            new AssignTherapistRequest { TherapistProfileId = profileId });

        var clientId2 = await CreateClientAsync(client);
        var response = await client.PostAsJsonAsync($"/api/clients/{clientId2}/therapist",
            new AssignTherapistRequest { TherapistProfileId = profileId });
        var body = await response.Content.ReadFromJsonAsync<ProblemDetails>(JsonOptions);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(400, body.Status);
        Assert.Contains("caseload", body.Detail, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    [Trait("Story", "MN-26")]
    [Trait("AC", "AC-3")]
    public async Task AssignTherapist_Reassign_ReplacesPrevious()
    {
        var (client, userId) = await SetupAuthenticatedAsync();
        var clientId = await CreateClientAsync(client);
        var profileId1 = await CreateTherapistProfileAsync(client, userId);

        await client.PostAsJsonAsync($"/api/clients/{clientId}/therapist",
            new AssignTherapistRequest { TherapistProfileId = profileId1 });

        var (client2, userId2) = await SetupAuthenticatedAsync();
        var profileId2 = await CreateTherapistProfileAsync(client2, userId2);

        var response = await client.PostAsJsonAsync($"/api/clients/{clientId}/therapist",
            new AssignTherapistRequest { TherapistProfileId = profileId2 });
        var body = await response.Content.ReadFromJsonAsync<ClientResponse>(JsonOptions);

        Assert.Equal(profileId2, body.AssignedTherapistId);
    }

    [Fact]
    [Trait("Story", "MN-26")]
    [Trait("AC", "AC-4")]
    public async Task UnassignTherapist_RemovesAssignment()
    {
        var (client, userId) = await SetupAuthenticatedAsync();
        var clientId = await CreateClientAsync(client);
        var profileId = await CreateTherapistProfileAsync(client, userId);

        await client.PostAsJsonAsync($"/api/clients/{clientId}/therapist",
            new AssignTherapistRequest { TherapistProfileId = profileId });

        var response = await client.DeleteAsync($"/api/clients/{clientId}/therapist");
        var body = await response.Content.ReadFromJsonAsync<ClientResponse>(JsonOptions);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Null(body.AssignedTherapistId);
    }

    [Fact]
    [Trait("Story", "MN-26")]
    [Trait("AC", "AC-5")]
    public async Task GetClient_IncludesAssignedTherapistId()
    {
        var (client, userId) = await SetupAuthenticatedAsync();
        var clientId = await CreateClientAsync(client);
        var profileId = await CreateTherapistProfileAsync(client, userId);

        await client.PostAsJsonAsync($"/api/clients/{clientId}/therapist",
            new AssignTherapistRequest { TherapistProfileId = profileId });

        var response = await client.GetAsync($"/api/clients/{clientId}");
        var body = await response.Content.ReadFromJsonAsync<ClientResponse>(JsonOptions);

        Assert.Equal(profileId, body.AssignedTherapistId);
    }

    [Fact]
    [Trait("Story", "MN-26")]
    [Trait("AC", "AC-6")]
    public async Task AssignTherapist_NonExistentClient_ReturnsProblemDetails()
    {
        var (client, _) = await SetupAuthenticatedAsync();

        var response = await client.PostAsJsonAsync($"/api/clients/{Guid.NewGuid()}/therapist",
            new AssignTherapistRequest { TherapistProfileId = Guid.NewGuid() });
        var body = await response.Content.ReadFromJsonAsync<ProblemDetails>(JsonOptions);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(404, body.Status);
    }

    [Fact]
    [Trait("Story", "MN-26")]
    [Trait("AC", "AC-7")]
    public async Task AssignTherapist_NonExistentProfile_ReturnsProblemDetails()
    {
        var (client, _) = await SetupAuthenticatedAsync();
        var clientId = await CreateClientAsync(client);

        var response = await client.PostAsJsonAsync($"/api/clients/{clientId}/therapist",
            new AssignTherapistRequest { TherapistProfileId = Guid.NewGuid() });
        var body = await response.Content.ReadFromJsonAsync<ProblemDetails>(JsonOptions);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(400, body.Status);
    }

    [Fact]
    [Trait("Story", "MN-26")]
    [Trait("AC", "AC-8")]
    public async Task AssignAndUnassign_Unauthenticated_Returns401()
    {
        var unauthClient = _fixture.Factory.CreateClient();

        var assignResponse = await unauthClient.PostAsJsonAsync($"/api/clients/{Guid.NewGuid()}/therapist",
            new AssignTherapistRequest { TherapistProfileId = Guid.NewGuid() });
        var unassignResponse = await unauthClient.DeleteAsync($"/api/clients/{Guid.NewGuid()}/therapist");

        Assert.Equal(HttpStatusCode.Unauthorized, assignResponse.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, unassignResponse.StatusCode);
    }
}
