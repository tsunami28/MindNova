using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using MindNova.Api.Contracts;
using MindNova.Api.Tests.Auth;

namespace MindNova.Api.Tests.Therapists;

[Collection("SqlServer")]
public class TherapistEndpointTests
{
    private readonly SqlServerFixture _fixture;
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public TherapistEndpointTests(SqlServerFixture fixture)
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
        var email = $"therapist-ep-{Guid.NewGuid():N}@example.com";
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

    private CreateTherapistRequest ValidRequest(string userId) => new()
    {
        UserId = userId,
        Specialisations = new List<string> { "CBT", "EMDR" },
        MaxCaseload = 10
    };

    [Fact]
    [Trait("Story", "MN-25")]
    [Trait("AC", "AC-1")]
    public async Task Post_ValidData_ReturnsCreatedProfile()
    {
        var (client, userId) = await SetupAuthenticatedAsync();
        var request = ValidRequest(userId);

        var response = await client.PostAsJsonAsync("/api/therapists", request);
        var body = await response.Content.ReadFromJsonAsync<TherapistProfileResponse>(JsonOptions);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotEqual(Guid.Empty, body.Id);
        Assert.Equal(userId, body.UserId);
        Assert.Equal(new List<string> { "CBT", "EMDR" }, body.Specialisations);
        Assert.Equal(10, body.MaxCaseload);
        Assert.True(body.IsActive);
    }

    [Fact]
    [Trait("Story", "MN-25")]
    [Trait("AC", "AC-2")]
    public async Task Post_NonExistentUserId_ReturnsProblemDetails()
    {
        var (client, _) = await SetupAuthenticatedAsync();
        var request = new CreateTherapistRequest
        {
            UserId = "non-existent-user-id",
            Specialisations = new List<string> { "CBT" },
            MaxCaseload = 5
        };

        var response = await client.PostAsJsonAsync("/api/therapists", request);
        var body = await response.Content.ReadFromJsonAsync<ProblemDetails>(JsonOptions);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(400, body.Status);
    }

    [Fact]
    [Trait("Story", "MN-25")]
    [Trait("AC", "AC-3")]
    public async Task Post_DuplicateUserId_ReturnsProblemDetails()
    {
        var (client, userId) = await SetupAuthenticatedAsync();
        var request = ValidRequest(userId);

        await client.PostAsJsonAsync("/api/therapists", request);
        var response = await client.PostAsJsonAsync("/api/therapists", request);
        var body = await response.Content.ReadFromJsonAsync<ProblemDetails>(JsonOptions);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(400, body.Status);
        Assert.Contains("already exists", body.Detail);
    }

    [Fact]
    [Trait("Story", "MN-25")]
    [Trait("AC", "AC-4")]
    public async Task GetById_ExistingProfile_ReturnsFullData()
    {
        var (client, userId) = await SetupAuthenticatedAsync();
        var createResponse = await client.PostAsJsonAsync("/api/therapists", ValidRequest(userId));
        var created = await createResponse.Content.ReadFromJsonAsync<TherapistProfileResponse>(JsonOptions);

        var response = await client.GetAsync($"/api/therapists/{created.Id}");
        var body = await response.Content.ReadFromJsonAsync<TherapistProfileResponse>(JsonOptions);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(created.Id, body.Id);
        Assert.Equal(userId, body.UserId);
        Assert.Equal(new List<string> { "CBT", "EMDR" }, body.Specialisations);
    }

    [Fact]
    [Trait("Story", "MN-25")]
    [Trait("AC", "AC-5")]
    public async Task GetById_NonExistent_ReturnsProblemDetails()
    {
        var (client, _) = await SetupAuthenticatedAsync();

        var response = await client.GetAsync($"/api/therapists/{Guid.NewGuid()}");
        var body = await response.Content.ReadFromJsonAsync<ProblemDetails>(JsonOptions);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(404, body.Status);
    }

    [Fact]
    [Trait("Story", "MN-25")]
    [Trait("AC", "AC-6")]
    public async Task List_ReturnsPagedActiveProfiles()
    {
        var (client, userId) = await SetupAuthenticatedAsync();
        await client.PostAsJsonAsync("/api/therapists", ValidRequest(userId));

        var response = await client.GetAsync("/api/therapists");
        var body = await response.Content.ReadFromJsonAsync<PagedResponse<TherapistProfileResponse>>(JsonOptions);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.True(body.Items.Count >= 1);
        Assert.True(body.Items.All(t => t.IsActive));
        Assert.Equal(1, body.Page);
        Assert.Equal(20, body.PageSize);
    }

    [Fact]
    [Trait("Story", "MN-25")]
    [Trait("AC", "AC-7")]
    public async Task List_IncludeInactive_ReturnsAll()
    {
        var (client, userId) = await SetupAuthenticatedAsync();
        var createResponse = await client.PostAsJsonAsync("/api/therapists", ValidRequest(userId));
        var created = await createResponse.Content.ReadFromJsonAsync<TherapistProfileResponse>(JsonOptions);

        await client.DeleteAsync($"/api/therapists/{created.Id}");

        var response = await client.GetAsync("/api/therapists?include_inactive=true&page_size=100");
        var body = await response.Content.ReadFromJsonAsync<PagedResponse<TherapistProfileResponse>>(JsonOptions);

        Assert.Contains(body.Items, t => t.Id == created.Id && !t.IsActive);
    }

    [Fact]
    [Trait("Story", "MN-25")]
    [Trait("AC", "AC-8")]
    public async Task Put_UpdatesSpecialisationsAndMaxCaseload()
    {
        var (client, userId) = await SetupAuthenticatedAsync();
        var createResponse = await client.PostAsJsonAsync("/api/therapists", ValidRequest(userId));
        var created = await createResponse.Content.ReadFromJsonAsync<TherapistProfileResponse>(JsonOptions);

        var updateRequest = new UpdateTherapistRequest
        {
            Specialisations = new List<string> { "DBT", "ACT", "Schema" },
            MaxCaseload = 15
        };
        var response = await client.PutAsJsonAsync($"/api/therapists/{created.Id}", updateRequest);
        var body = await response.Content.ReadFromJsonAsync<TherapistProfileResponse>(JsonOptions);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(new List<string> { "DBT", "ACT", "Schema" }, body.Specialisations);
        Assert.Equal(15, body.MaxCaseload);
    }

    [Fact]
    [Trait("Story", "MN-25")]
    [Trait("AC", "AC-9")]
    public async Task Delete_SoftDeactivates()
    {
        var (client, userId) = await SetupAuthenticatedAsync();
        var createResponse = await client.PostAsJsonAsync("/api/therapists", ValidRequest(userId));
        var created = await createResponse.Content.ReadFromJsonAsync<TherapistProfileResponse>(JsonOptions);

        var response = await client.DeleteAsync($"/api/therapists/{created.Id}");
        var body = await response.Content.ReadFromJsonAsync<TherapistProfileResponse>(JsonOptions);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.False(body.IsActive);
    }

    [Fact]
    [Trait("Story", "MN-25")]
    [Trait("AC", "AC-10")]
    public async Task AllEndpoints_Unauthenticated_Returns401()
    {
        var unauthClient = _fixture.Factory.CreateClient();

        var postResponse = await unauthClient.PostAsJsonAsync("/api/therapists", new CreateTherapistRequest());
        var getListResponse = await unauthClient.GetAsync("/api/therapists");
        var getByIdResponse = await unauthClient.GetAsync($"/api/therapists/{Guid.NewGuid()}");
        var putResponse = await unauthClient.PutAsJsonAsync($"/api/therapists/{Guid.NewGuid()}", new UpdateTherapistRequest());
        var deleteResponse = await unauthClient.DeleteAsync($"/api/therapists/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.Unauthorized, postResponse.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, getListResponse.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, getByIdResponse.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, putResponse.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, deleteResponse.StatusCode);
    }
}
