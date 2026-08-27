using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Components.Authorization;

namespace MindNova.Web.Services;

public class AuthService
{
    private readonly HttpClient _httpClient;
    private readonly JwtAuthenticationStateProvider _authStateProvider;
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public AuthService(HttpClient httpClient, AuthenticationStateProvider authStateProvider)
    {
        _httpClient = httpClient;
        _authStateProvider = (JwtAuthenticationStateProvider)authStateProvider;
    }

    public async Task<(bool Success, string Error)> LoginAsync(string email, string password)
    {
        var response = await _httpClient.PostAsJsonAsync("/api/auth/login", new { Email = email, Password = password });
        var content = await response.Content.ReadAsStringAsync();

        var result = JsonSerializer.Deserialize<LoginResponse>(content, JsonOptions);

        if (result == null || string.IsNullOrEmpty(result.Token))
            return (false, result?.Errors?.FirstOrDefault() ?? "Login failed.");

        _authStateProvider.SetToken(result.Token);
        _httpClient.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", result.Token);

        return (true, null);
    }

    public Task LogoutAsync()
    {
        _authStateProvider.ClearToken();
        _httpClient.DefaultRequestHeaders.Authorization = null;
        return Task.CompletedTask;
    }

    private class LoginResponse
    {
        public bool Succeeded { get; set; }
        public string Token { get; set; } = string.Empty;
        public List<string> Errors { get; set; } = new();
    }
}
