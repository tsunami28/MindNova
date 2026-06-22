using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using MindNova.Api.Contracts;
using MindNova.Api.Tests.Auth;

namespace MindNova.Api.Tests.Clients;

[Collection("SqlServer")]
public class ClientEndpointTests
{
    private readonly SqlServerFixture _fixture;
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public ClientEndpointTests(SqlServerFixture fixture)
    {
        _fixture = fixture;
    }

    private class TokenResponse
    {
        public string Token { get; set; } = string.Empty;
    }

    private async Task<string> GetTokenAsync()
    {
        var email = $"test-{Guid.NewGuid():N}@example.com";
        await _fixture.Client.PostAsJsonAsync("/api/auth/register", new { Email = email, Password = "Test1234!" });
        var loginResponse = await _fixture.Client.PostAsJsonAsync("/api/auth/login", new { Email = email, Password = "Test1234!" });
        var loginBody = await loginResponse.Content.ReadFromJsonAsync<TokenResponse>(JsonOptions);
        return loginBody.Token;
    }

    private HttpClient CreateAuthenticatedClient(string token)
    {
        var client = _fixture.Factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    private static CreateClientRequest ValidCreateRequest() => new()
    {
        FirstName = "Jane",
        LastName = "Smith",
        Email = "jane.smith@example.com",
        DateOfBirth = new DateTime(1985, 3, 20),
        Phone = "+31612345678",
        EmergencyContactName = "John Smith",
        EmergencyContactPhone = "+31698765432",
        Address = "123 Main Street"
    };

    [Fact]
    [Trait("Story", "MN-14")]
    [Trait("AC", "AC-1")]
    public async Task Post_ValidClient_ReturnsCreatedClientWithGeneratedFields()
    {
        var token = await GetTokenAsync();
        using var client = CreateAuthenticatedClient(token);
        var request = ValidCreateRequest();

        var response = await client.PostAsJsonAsync("/api/clients", request);
        var body = await response.Content.ReadFromJsonAsync<ClientResponse>(JsonOptions);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotEqual(Guid.Empty, body.Id);
        Assert.Equal("Jane", body.FirstName);
        Assert.Equal("jane.smith@example.com", body.Email);
        Assert.NotEqual(default, body.CreatedAt);
        Assert.False(body.IsArchived);
    }

    [Fact]
    [Trait("Story", "MN-14")]
    [Trait("AC", "AC-2")]
    public async Task Post_MissingRequiredFields_ReturnsProblemDetails()
    {
        var token = await GetTokenAsync();
        using var client = CreateAuthenticatedClient(token);
        var request = new CreateClientRequest { FirstName = "", LastName = "", Email = "" };

        var response = await client.PostAsJsonAsync("/api/clients", request);
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("FirstName", body);
    }

    [Fact]
    [Trait("Story", "MN-14")]
    [Trait("AC", "AC-3")]
    public async Task Post_InvalidEmail_ReturnsProblemDetails()
    {
        var token = await GetTokenAsync();
        using var client = CreateAuthenticatedClient(token);
        var request = ValidCreateRequest();
        request.Email = "not-a-valid-email";

        var response = await client.PostAsJsonAsync("/api/clients", request);
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("Email", body);
    }

    [Fact]
    [Trait("Story", "MN-14")]
    [Trait("AC", "AC-4")]
    public async Task GetById_ExistingClient_ReturnsFullData()
    {
        var token = await GetTokenAsync();
        using var client = CreateAuthenticatedClient(token);
        var request = ValidCreateRequest();
        request.Email = $"get-{Guid.NewGuid():N}@example.com";
        var createResponse = await client.PostAsJsonAsync("/api/clients", request);
        var created = await createResponse.Content.ReadFromJsonAsync<ClientResponse>(JsonOptions);

        var response = await client.GetAsync($"/api/clients/{created.Id}");
        var body = await response.Content.ReadFromJsonAsync<ClientResponse>(JsonOptions);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(created.Id, body.Id);
        Assert.Equal(request.FirstName, body.FirstName);
        Assert.Equal(request.LastName, body.LastName);
    }

    [Fact]
    [Trait("Story", "MN-14")]
    [Trait("AC", "AC-5")]
    public async Task GetById_NonExistentId_ReturnsProblemDetails()
    {
        var token = await GetTokenAsync();
        using var client = CreateAuthenticatedClient(token);

        var response = await client.GetAsync($"/api/clients/{Guid.NewGuid()}");
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("not found", body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    [Trait("Story", "MN-14")]
    [Trait("AC", "AC-6")]
    public async Task GetList_ReturnsNonArchivedClients()
    {
        var token = await GetTokenAsync();
        using var client = CreateAuthenticatedClient(token);
        var request = ValidCreateRequest();
        request.Email = $"list-{Guid.NewGuid():N}@example.com";
        await client.PostAsJsonAsync("/api/clients", request);

        var response = await client.GetAsync("/api/clients");
        var body = await response.Content.ReadFromJsonAsync<List<ClientResponse>>(JsonOptions);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(body);
        Assert.All(body, c => Assert.False(c.IsArchived));
    }

    [Fact]
    [Trait("Story", "MN-14")]
    [Trait("AC", "AC-7")]
    public async Task Put_ValidData_UpdatesClientAndRefreshesUpdatedAt()
    {
        var token = await GetTokenAsync();
        using var client = CreateAuthenticatedClient(token);
        var request = ValidCreateRequest();
        request.Email = $"update-{Guid.NewGuid():N}@example.com";
        var createResponse = await client.PostAsJsonAsync("/api/clients", request);
        var created = await createResponse.Content.ReadFromJsonAsync<ClientResponse>(JsonOptions);

        var updateRequest = new UpdateClientRequest
        {
            FirstName = "Updated",
            LastName = "Name",
            Email = request.Email,
            DateOfBirth = request.DateOfBirth,
            Phone = request.Phone,
            EmergencyContactName = request.EmergencyContactName,
            EmergencyContactPhone = request.EmergencyContactPhone,
            Address = "456 New Street"
        };

        var response = await client.PutAsJsonAsync($"/api/clients/{created.Id}", updateRequest);
        var body = await response.Content.ReadFromJsonAsync<ClientResponse>(JsonOptions);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("Updated", body.FirstName);
        Assert.Equal("456 New Street", body.Address);
        Assert.True(body.UpdatedAt > created.UpdatedAt);
    }

    [Fact]
    [Trait("Story", "MN-14")]
    [Trait("AC", "AC-8")]
    public async Task Put_NonExistentId_ReturnsProblemDetails()
    {
        var token = await GetTokenAsync();
        using var client = CreateAuthenticatedClient(token);
        var updateRequest = new UpdateClientRequest
        {
            FirstName = "Test",
            LastName = "User",
            Email = "test@example.com"
        };

        var response = await client.PutAsJsonAsync($"/api/clients/{Guid.NewGuid()}", updateRequest);
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("not found", body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    [Trait("Story", "MN-14")]
    [Trait("AC", "AC-9")]
    public async Task Delete_ExistingClient_SetsIsArchivedTrue()
    {
        var token = await GetTokenAsync();
        using var client = CreateAuthenticatedClient(token);
        var request = ValidCreateRequest();
        request.Email = $"archive-{Guid.NewGuid():N}@example.com";
        var createResponse = await client.PostAsJsonAsync("/api/clients", request);
        var created = await createResponse.Content.ReadFromJsonAsync<ClientResponse>(JsonOptions);

        var response = await client.DeleteAsync($"/api/clients/{created.Id}");
        var body = await response.Content.ReadFromJsonAsync<ClientResponse>(JsonOptions);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.True(body.IsArchived);
    }

    [Fact]
    [Trait("Story", "MN-14")]
    [Trait("AC", "AC-10")]
    public async Task ArchivedClient_ExcludedFromList_ButRetrievableById()
    {
        var token = await GetTokenAsync();
        using var client = CreateAuthenticatedClient(token);
        var request = ValidCreateRequest();
        request.Email = $"excluded-{Guid.NewGuid():N}@example.com";
        var createResponse = await client.PostAsJsonAsync("/api/clients", request);
        var created = await createResponse.Content.ReadFromJsonAsync<ClientResponse>(JsonOptions);

        await client.DeleteAsync($"/api/clients/{created.Id}");

        var listResponse = await client.GetAsync("/api/clients");
        var list = await listResponse.Content.ReadFromJsonAsync<List<ClientResponse>>(JsonOptions);
        Assert.DoesNotContain(list, c => c.Id == created.Id);

        var getResponse = await client.GetAsync($"/api/clients/{created.Id}");
        var retrieved = await getResponse.Content.ReadFromJsonAsync<ClientResponse>(JsonOptions);
        Assert.Equal(created.Id, retrieved.Id);
        Assert.True(retrieved.IsArchived);
    }

    [Fact]
    [Trait("Story", "MN-14")]
    [Trait("AC", "AC-11")]
    public async Task AllEndpoints_WithoutToken_Return401()
    {
        var unauthenticatedClient = _fixture.Factory.CreateClient();

        var postResponse = await unauthenticatedClient.PostAsJsonAsync("/api/clients", ValidCreateRequest());
        Assert.Equal(HttpStatusCode.Unauthorized, postResponse.StatusCode);

        var getResponse = await unauthenticatedClient.GetAsync($"/api/clients/{Guid.NewGuid()}");
        Assert.Equal(HttpStatusCode.Unauthorized, getResponse.StatusCode);

        var listResponse = await unauthenticatedClient.GetAsync("/api/clients");
        Assert.Equal(HttpStatusCode.Unauthorized, listResponse.StatusCode);

        var putResponse = await unauthenticatedClient.PutAsJsonAsync($"/api/clients/{Guid.NewGuid()}", ValidCreateRequest());
        Assert.Equal(HttpStatusCode.Unauthorized, putResponse.StatusCode);

        var deleteResponse = await unauthenticatedClient.DeleteAsync($"/api/clients/{Guid.NewGuid()}");
        Assert.Equal(HttpStatusCode.Unauthorized, deleteResponse.StatusCode);
    }
}
