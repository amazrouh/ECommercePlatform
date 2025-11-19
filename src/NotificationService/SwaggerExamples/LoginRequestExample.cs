using NotificationService.DTOs;
using Swashbuckle.AspNetCore.Filters;

namespace NotificationService.SwaggerExamples;

/// <summary>
/// Swagger examples for LoginRequest
/// </summary>
public class LoginRequestExample : IMultipleExamplesProvider<LoginRequest>
{
    /// <summary>
    /// Get examples for LoginRequest
    /// </summary>
    /// <returns></returns>
    public IEnumerable<SwaggerExample<LoginRequest>> GetExamples()
    {
        yield return SwaggerExample.Create(
            "Admin Login",
            new LoginRequest
            {
                Username = "admin",
                Password = "admin123"
            });

        yield return SwaggerExample.Create(
            "User Login",
            new LoginRequest
            {
                Username = "user",
                Password = "user123"
            });
    }
}

/// <summary>
/// Swagger examples for LoginResponse
/// </summary>
public class LoginResponseExample : IMultipleExamplesProvider<LoginResponse>
{
    /// <summary>
    /// Get examples for LoginResponse
    /// </summary>
    /// <returns></returns>
    public IEnumerable<SwaggerExample<LoginResponse>> GetExamples()
    {
        yield return SwaggerExample.Create(
            "Successful Login",
            new LoginResponse
            {
                Token = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
                ExpiresIn = 3600,
                TokenType = "Bearer"
            });
    }
}
