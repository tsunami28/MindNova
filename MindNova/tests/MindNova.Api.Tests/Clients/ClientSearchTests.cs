using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using MindNova.Api.Contracts;
using MindNova.Api.Tests.Auth;

namespace MindNova.Api.Tests.Clients;

[Collection("SqlServer")]
public class ClientSearchTests
{
    private readonly SqlServerFixture _fixture;
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public ClientSearchTests(SqlServerFixture fixture)
    {
        _fixture = fixture;
    }

    private class TokenResponse
    {
        public string Token { get; set; } = string.Empty;
    }

    private async Task<string> GetTokenAsync()
    {
        var email = $"search-{Guid.NewGuid():N}@example.com";
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

    private async Task<ClientResponse> CreateClientAsync(HttpClient client, string firstName, string lastName, string email)
    {
        var request = new CreateClientRequest
        {
            FirstName = firstName,
            LastName = lastName,
            Email = email,
            DateOfBirth = new DateTime(1990, 1, 1),
            Phone = "+31600000000"
        };
        var response = await client.PostAsJsonAsync("/api/clients", request);
        return await response.Content.ReadFromJsonAsync<ClientResponse>(JsonOptions);
    }

    [Fact]
    [Trait("Story", "MN-15")]
    [Trait("AC", "AC-1")]
    public async Task Search_ByFirstName_ReturnsMatchingClients()
    {
        var token = await GetTokenAsync();
        using var client = CreateAuthenticatedClient(token);
        var unique = Guid.NewGuid().ToString("N")[..8];
        await CreateClientAsync(client, $"Zzfind{unique}", "Doe", $"find{unique}@example.com");
        await CreateClientAsync(client, "Other", "Person", $"other{unique}@example.com");

        var response = await client.GetAsync($"/api/clients?search=Zzfind{unique}");
        var body = await response.Content.ReadFromJsonAsync<PagedResponse<ClientResponse>>(JsonOptions);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Single(body.Items);
        Assert.Contains(body.Items, c => c.FirstName == $"Zzfind{unique}");
    }

    [Fact]
    [Trait("Story", "MN-15")]
    [Trait("AC", "AC-1")]
    public async Task Search_ByEmail_ReturnsMatchingClients()
    {
        var token = await GetTokenAsync();
        using var client = CreateAuthenticatedClient(token);
        var unique = Guid.NewGuid().ToString("N")[..8];
        await CreateClientAsync(client, "Email", "Test", $"unique{unique}@special.com");

        var response = await client.GetAsync($"/api/clients?search=unique{unique}");
        var body = await response.Content.ReadFromJsonAsync<PagedResponse<ClientResponse>>(JsonOptions);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains(body.Items, c => c.Email == $"unique{unique}@special.com");
    }

    [Fact]
    [Trait("Story", "MN-15")]
    [Trait("AC", "AC-2")]
    public async Task Search_NoMatch_ReturnsEmptyItems()
    {
        var token = await GetTokenAsync();
        using var client = CreateAuthenticatedClient(token);

        var response = await client.GetAsync("/api/clients?search=zzzznonexistent999");
        var body = await response.Content.ReadFromJsonAsync<PagedResponse<ClientResponse>>(JsonOptions);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Empty(body.Items);
        Assert.Equal(0, body.TotalCount);
    }

    [Fact]
    [Trait("Story", "MN-15")]
    [Trait("AC", "AC-3")]
    public async Task List_DefaultPagination_ReturnsPage1WithPageSize20()
    {
        var token = await GetTokenAsync();
        using var client = CreateAuthenticatedClient(token);

        var response = await client.GetAsync("/api/clients");
        var body = await response.Content.ReadFromJsonAsync<PagedResponse<ClientResponse>>(JsonOptions);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(1, body.Page);
        Assert.Equal(20, body.PageSize);
    }

    [Fact]
    [Trait("Story", "MN-15")]
    [Trait("AC", "AC-4")]
    public async Task Pagination_ReturnsCorrectSliceAndMetadata()
    {
        var token = await GetTokenAsync();
        using var client = CreateAuthenticatedClient(token);
        var unique = Guid.NewGuid().ToString("N")[..8];

        for (int i = 0; i < 7; i++)
        {
            await CreateClientAsync(client, $"Page{unique}", $"Client{i:D2}", $"page{unique}{i}@example.com");
        }

        var response = await client.GetAsync($"/api/clients?search=Page{unique}&page=2&page_size=3");
        var body = await response.Content.ReadFromJsonAsync<PagedResponse<ClientResponse>>(JsonOptions);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(2, body.Page);
        Assert.Equal(3, body.PageSize);
        Assert.Equal(7, body.TotalCount);
        Assert.Equal(3, body.Items.Count);
    }

    [Fact]
    [Trait("Story", "MN-15")]
    [Trait("AC", "AC-5")]
    public async Task IncludeArchived_False_ExcludesArchivedClients()
    {
        var token = await GetTokenAsync();
        using var client = CreateAuthenticatedClient(token);
        var unique = Guid.NewGuid().ToString("N")[..8];
        var created = await CreateClientAsync(client, $"Archived{unique}", "Test", $"archived{unique}@example.com");
        await client.DeleteAsync($"/api/clients/{created.Id}");

        var response = await client.GetAsync($"/api/clients?search=Archived{unique}");
        var body = await response.Content.ReadFromJsonAsync<PagedResponse<ClientResponse>>(JsonOptions);

        Assert.Empty(body.Items);
    }

    [Fact]
    [Trait("Story", "MN-15")]
    [Trait("AC", "AC-5")]
    public async Task IncludeArchived_True_IncludesArchivedClients()
    {
        var token = await GetTokenAsync();
        using var client = CreateAuthenticatedClient(token);
        var unique = Guid.NewGuid().ToString("N")[..8];
        var created = await CreateClientAsync(client, $"Included{unique}", "Test", $"included{unique}@example.com");
        await client.DeleteAsync($"/api/clients/{created.Id}");

        var response = await client.GetAsync($"/api/clients?search=Included{unique}&include_archived=true");
        var body = await response.Content.ReadFromJsonAsync<PagedResponse<ClientResponse>>(JsonOptions);

        Assert.Single(body.Items);
        Assert.True(body.Items[0].IsArchived);
    }

    [Fact]
    [Trait("Story", "MN-15")]
    [Trait("AC", "AC-6")]
    public async Task Response_HasPagedResponseShape()
    {
        var token = await GetTokenAsync();
        using var client = CreateAuthenticatedClient(token);

        var response = await client.GetAsync("/api/clients");
        var body = await response.Content.ReadFromJsonAsync<PagedResponse<ClientResponse>>(JsonOptions);

        Assert.NotNull(body.Items);
        Assert.True(body.TotalCount >= 0);
        Assert.True(body.Page >= 1);
        Assert.True(body.PageSize >= 1);
    }

    [Fact]
    [Trait("Story", "MN-15")]
    [Trait("AC", "AC-7")]
    public async Task List_WithoutToken_Returns401()
    {
        var unauthenticatedClient = _fixture.Factory.CreateClient();

        var response = await unauthenticatedClient.GetAsync("/api/clients?search=test");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
