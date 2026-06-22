using Microsoft.AspNetCore.Mvc;
using MindNova.Infrastructure.Auth;

namespace MindNova.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        var result = await _authService.RegisterAsync(request.Email, request.Password);

        if (!result.Succeeded)
        {
            return Ok(new ProblemDetails
            {
                Title = "Registration failed",
                Detail = string.Join("; ", result.Errors),
                Status = 400
            });
        }

        return Ok(new { Message = "User registered successfully." });
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var result = await _authService.LoginAsync(request.Email, request.Password);

        if (!result.Succeeded)
        {
            return Ok(new ProblemDetails
            {
                Title = "Login failed",
                Detail = string.Join("; ", result.Errors),
                Status = 401
            });
        }

        return Ok(new { Token = result.Token });
    }
}

public class RegisterRequest
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public class LoginRequest
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}
