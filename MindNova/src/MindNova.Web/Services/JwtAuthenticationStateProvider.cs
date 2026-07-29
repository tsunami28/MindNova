using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;

namespace MindNova.Web.Services;

public class JwtAuthenticationStateProvider : AuthenticationStateProvider
{
    private string _token = string.Empty;

    public void SetToken(string token)
    {
        _token = token;
        NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
    }

    public void ClearToken()
    {
        _token = string.Empty;
        NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
    }

    public override Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        if (string.IsNullOrEmpty(_token))
        {
            return Task.FromResult(new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity())));
        }

        var handler = new JwtSecurityTokenHandler();
        var jwt = handler.ReadJwtToken(_token);

        var claims = jwt.Claims.ToList();

        // Map JWT role claims to the ClaimTypes.Role format Blazor expects
        var roleClaims = claims
            .Where(c => c.Type == ClaimTypes.Role || c.Type == "role")
            .Select(c => new Claim(ClaimTypes.Role, c.Value))
            .ToList();

        var identity = new ClaimsIdentity(claims.Concat(roleClaims).DistinctBy(c => c.Type + c.Value), "jwt");
        var user = new ClaimsPrincipal(identity);

        return Task.FromResult(new AuthenticationState(user));
    }
}
