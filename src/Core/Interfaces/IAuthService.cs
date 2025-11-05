namespace Core.Interfaces;

/// <summary>
/// Interface for authentication operations
/// </summary>
public interface IAuthService
{
    /// <summary>
    /// Authenticates a user and returns a JWT token if successful
    /// </summary>
    /// <param name="username">The username to authenticate</param>
    /// <param name="password">The password for authentication</param>
    /// <returns>A JWT token if authentication is successful, null otherwise</returns>
    Task<string?> AuthenticateAsync(string username, string password);

    /// <summary>
    /// Validates user credentials
    /// </summary>
    /// <param name="username">The username to validate</param>
    /// <param name="password">The password to validate</param>
    /// <returns>True if credentials are valid, false otherwise</returns>
    Task<bool> ValidateCredentialsAsync(string username, string password);

    /// <summary>
    /// Gets the user role for the given username
    /// </summary>
    /// <param name="username">The username to get role for</param>
    /// <returns>The user role if found, null otherwise</returns>
    Task<string?> GetUserRoleAsync(string username);
}
