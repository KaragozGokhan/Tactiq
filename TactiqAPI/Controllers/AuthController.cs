using Microsoft.AspNetCore.Mvc;
using TactiqAPI.DTOs;
using TactiqAPI.Services;

namespace TactiqAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
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
        var (success, message, user) = await _authService.RegisterAsync(request.Username, request.Email, request.Password);

        if (!success)
            return BadRequest(new AuthResponse { Success = false, Message = message });

        var token = _authService.GenerateJwtToken(user!);
        var userDto = new UserDto
        {
            Id = user!.Id,
            Username = user!.Username,
            Email = user!.Email,
            Role = user!.Role
        };

        return Ok(new AuthResponse
        {
            Success = true,
            Message = message,
            Token = token,
            User = userDto
        });
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var (success, message, user) = await _authService.LoginAsync(request.Email, request.Password);

        if (!success)
            return Unauthorized(new AuthResponse { Success = false, Message = message });

        var token = _authService.GenerateJwtToken(user!);
        var userDto = new UserDto
        {
            Id = user!.Id,
            Username = user!.Username,
            Email = user!.Email,
            Role = user!.Role
        };

        return Ok(new AuthResponse
        {
            Success = true,
            Message = message,
            Token = token,
            User = userDto
        });
    }
}
