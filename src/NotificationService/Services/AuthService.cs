using Core.Interfaces;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using NotificationService.Configurations;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace NotificationService.Services;

/// <summary>
/// Implementation of authentication service using JWT tokens
/// </summary>
public class AuthService : IAuthService
{
    private readonly JwtConfig _jwtConfig;

    // Simple in-memory user store for demonstration
    // In production, this would be replaced with database lookups
    private readonly Dictionary<string, (string Password, string Role)> _users = new()
    {
        { "admin", ("admin123", "Admin") },
        { "user", ("user123", "User") }
    };

    public AuthService(IOptions<JwtConfig> jwtConfig)
    {
        _jwtConfig = jwtConfig.Value;
    }

    /// <inheritdoc/>
    public async Task<string?> AuthenticateAsync(string username, string password)
    {
        if (!await ValidateCredentialsAsync(username, password))
        {
            return null;
        }

        var role = await GetUserRoleAsync(username);
        if (role == null)
        {
            return null;
        }

        return GenerateJwtToken(username, role);
    }

    /// <inheritdoc/>
    public async Task<bool> ValidateCredentialsAsync(string username, string password)
    {
        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
        {
            return false;
        }

        return _users.TryGetValue(username, out var userCredentials) &&
               userCredentials.Password == password;
    }

    /// <inheritdoc/>
    public async Task<string?> GetUserRoleAsync(string username)
    {
        if (_users.TryGetValue(username, out var userCredentials))
        {
            return userCredentials.Role;
        }

        return null;
    }

    private string GenerateJwtToken(string username, string role)
    {
        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtConfig.Secret));
        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, username),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new Claim(ClaimTypes.Name, username),
            new Claim(ClaimTypes.Role, role),
            new Claim(JwtRegisteredClaimNames.Iat, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString())
        };

        var token = new JwtSecurityToken(
            issuer: _jwtConfig.Issuer,
            audience: _jwtConfig.Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(_jwtConfig.ExpirationMinutes),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
