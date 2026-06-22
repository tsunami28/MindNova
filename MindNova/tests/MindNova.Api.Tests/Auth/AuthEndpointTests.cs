using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MindNova.Infrastructure.Data;
using Testcontainers.MsSql;

namespace MindNova.Api.Tests.Auth;

[Collection("SqlServer")]
public class AuthEndpointTests
{
    private readonly SqlServerFixture _fixture;
    private readonly HttpClient _client;
    private readonly WebApplicationFactory<Program> _factory;

    public AuthEndpointTests(SqlServerFixture fixture)
    {
        _fixture = fixture;
        _client = fixture.Client;
        _factory = fixture.Factory;
    }

    [Fact]
    [Trait("Story", "MN-10")]
    [Trait("AC", "AC-1")]
    public async Task Identity_Migration_Creates_Identity_Tables()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<MindNovaDbContext>();

        var tables = new[] { "AspNetUsers", "AspNetRoles", "AspNetUserRoles",
                             "AspNetUserClaims", "AspNetRoleClaims", "AspNetUserLogins",
                             "AspNetUserTokens" };

        foreach (var table in tables)
        {
            var count = await db.Database.SqlQueryRaw<int>(
                "SELECT COUNT(*) AS [Value] FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = {0}", table)
                .FirstOrDefaultAsync();
            Assert.True(count > 0, $"Table {table} should exist in the database.");
        }
    }

    [Fact]
    [Trait("Story", "MN-10")]
    [Trait("AC", "AC-2")]
    public async Task Register_With_Valid_Email_And_Password_Returns_200_And_Persists_User()
    {
        var response = await _client.PostAsJsonAsync("/api/auth/register",
            new { Email = "newuser@test.com", Password = "ValidPass1" });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var scope = _factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<MindNova.Domain.Entities.ApplicationUser>>();
        var user = await userManager.FindByEmailAsync("newuser@test.com");
        Assert.NotNull(user);
    }

    [Fact]
    [Trait("Story", "MN-10")]
    [Trait("AC", "AC-3")]
    public async Task Register_With_Duplicate_Email_Returns_ProblemDetails()
    {
        await _client.PostAsJsonAsync("/api/auth/register",
            new { Email = "duplicate@test.com", Password = "ValidPass1" });

        var response = await _client.PostAsJsonAsync("/api/auth/register",
            new { Email = "duplicate@test.com", Password = "ValidPass1" });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("already exists", content);
    }

    [Fact]
    [Trait("Story", "MN-10")]
    [Trait("AC", "AC-4")]
    public async Task Register_With_Weak_Password_Returns_ProblemDetails_With_Violations()
    {
        var response = await _client.PostAsJsonAsync("/api/auth/register",
            new { Email = "weak@test.com", Password = "short" });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("Registration failed", content);
    }

    [Fact]
    [Trait("Story", "MN-10")]
    [Trait("AC", "AC-5")]
    public async Task Login_With_Valid_Credentials_Returns_200_With_Jwt()
    {
        await _client.PostAsJsonAsync("/api/auth/register",
            new { Email = "login@test.com", Password = "ValidPass1" });

        var response = await _client.PostAsJsonAsync("/api/auth/login",
            new { Email = "login@test.com", Password = "ValidPass1" });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("Token", content);
    }

    [Fact]
    [Trait("Story", "MN-10")]
    [Trait("AC", "AC-6")]
    public async Task Login_With_Invalid_Credentials_Returns_ProblemDetails()
    {
        var response = await _client.PostAsJsonAsync("/api/auth/login",
            new { Email = "nobody@test.com", Password = "WrongPass1" });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("Login failed", content);
    }

    [Fact]
    [Trait("Story", "MN-10")]
    [Trait("AC", "AC-7")]
    public async Task Jwt_Contains_Role_Claims()
    {
        await _client.PostAsJsonAsync("/api/auth/register",
            new { Email = "roled@test.com", Password = "ValidPass1" });

        using (var scope = _factory.Services.CreateScope())
        {
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<MindNova.Domain.Entities.ApplicationUser>>();
            var user = await userManager.FindByEmailAsync("roled@test.com");
            await userManager.AddToRoleAsync(user, "Admin");
        }

        var loginResponse = await _client.PostAsJsonAsync("/api/auth/login",
            new { Email = "roled@test.com", Password = "ValidPass1" });

        var loginContent = await loginResponse.Content.ReadFromJsonAsync<TokenResponse>();
        var handler = new JwtSecurityTokenHandler();
        var jwt = handler.ReadJwtToken(loginContent.Token);

        Assert.Contains(jwt.Claims, c => c.Type == ClaimTypes.Role && c.Value == "Admin");
    }

    [Fact]
    [Trait("Story", "MN-10")]
    [Trait("AC", "AC-8")]
    public async Task Request_Without_Token_Returns_401()
    {
        var response = await _client.GetAsync("/api/protected");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    [Trait("Story", "MN-10")]
    [Trait("AC", "AC-9")]
    public async Task Request_Without_Admin_Role_Returns_403()
    {
        await _client.PostAsJsonAsync("/api/auth/register",
            new { Email = "nonadmin@test.com", Password = "ValidPass1" });

        var loginResponse = await _client.PostAsJsonAsync("/api/auth/login",
            new { Email = "nonadmin@test.com", Password = "ValidPass1" });

        var loginContent = await loginResponse.Content.ReadFromJsonAsync<TokenResponse>();

        var request = new HttpRequestMessage(HttpMethod.Get, "/api/protected/admin");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", loginContent.Token);
        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    [Trait("Story", "MN-10")]
    [Trait("AC", "AC-10")]
    public async Task Default_Roles_Exist_After_Seeding()
    {
        using var scope = _factory.Services.CreateScope();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

        Assert.True(await roleManager.RoleExistsAsync("Admin"));
        Assert.True(await roleManager.RoleExistsAsync("Therapist"));
        Assert.True(await roleManager.RoleExistsAsync("Receptionist"));
    }

    private class TokenResponse
    {
        public string Token { get; set; } = string.Empty;
    }
}
