using Core.Interfaces;
using Microsoft.AspNetCore.Mvc;
using NotificationService.DTOs;

namespace NotificationService.Controllers;

/// <summary>
/// Controller for authentication operations
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly IAuditLogger _auditLogger;

    public AuthController(IAuthService authService, IAuditLogger auditLogger)
    {
        _authService = authService;
        _auditLogger = auditLogger;
    }

    /// <summary>
    /// Authenticates a user and returns a JWT token
    /// </summary>
    /// <param name="request">The login request containing username and password</param>
    /// <returns>A JWT token if authentication is successful</returns>
    /// <response code="200">Returns the JWT token</response>
    /// <response code="401">If authentication fails</response>
    [HttpPost("login")]
    [ProducesResponseType(typeof(LoginResponse), 200)]
    [ProducesResponseType(401)]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var token = await _authService.AuthenticateAsync(request.Username, request.Password);

        if (token == null)
        {
            // Log authentication failure
            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            await _auditLogger.LogAuthenticationFailureAsync(request.Username, ipAddress, "Invalid credentials");

            return Unauthorized(new { message = "Invalid username or password" });
        }

        // Log authentication success
        var successIpAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        await _auditLogger.LogAuthenticationSuccessAsync(request.Username, successIpAddress);

        var response = new LoginResponse
        {
            Token = token,
            ExpiresIn = 3600, // 1 hour in seconds
            TokenType = "Bearer"
        };

        return Ok(response);
    }
}
