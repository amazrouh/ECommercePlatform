using System.ComponentModel.DataAnnotations;

namespace Shared.Contracts.DTOs;

/// <summary>
/// User registration request.
/// </summary>
public record RegisterUserRequest
{
    [Required]
    [EmailAddress]
    [StringLength(256)]
    public string Email { get; init; } = string.Empty;

    [Required]
    [StringLength(100, MinimumLength = 8)]
    public string Password { get; init; } = string.Empty;

    [Required]
    [StringLength(100)]
    public string FirstName { get; init; } = string.Empty;

    [Required]
    [StringLength(100)]
    public string LastName { get; init; } = string.Empty;

    [Phone]
    public string? PhoneNumber { get; init; }
}

/// <summary>
/// User login request.
/// </summary>
public record LoginRequest
{
    [Required]
    [EmailAddress]
    public string Email { get; init; } = string.Empty;

    [Required]
    public string Password { get; init; } = string.Empty;
}

/// <summary>
/// Authentication response with tokens.
/// </summary>
public record AuthResponse
{
    public string AccessToken { get; init; } = string.Empty;
    public string RefreshToken { get; init; } = string.Empty;
    public DateTimeOffset ExpiresAt { get; init; }
    public UserDto User { get; init; } = null!;
}

/// <summary>
/// User data transfer object.
/// </summary>
public record UserDto
{
    public Guid Id { get; init; }
    public string Email { get; init; } = string.Empty;
    public string FirstName { get; init; } = string.Empty;
    public string LastName { get; init; } = string.Empty;
    public string FullName => $"{FirstName} {LastName}";
    public string? PhoneNumber { get; init; }
    public string Role { get; init; } = "Customer";
    public DateTimeOffset CreatedAt { get; init; }
    public bool IsActive { get; init; } = true;
}

/// <summary>
/// User profile update request.
/// </summary>
public record UpdateProfileRequest
{
    [StringLength(100)]
    public string? FirstName { get; init; }

    [StringLength(100)]
    public string? LastName { get; init; }

    [Phone]
    public string? PhoneNumber { get; init; }
}

/// <summary>
/// User address.
/// </summary>
public record AddressDto
{
    public Guid Id { get; init; }
    public string Street { get; init; } = string.Empty;
    public string City { get; init; } = string.Empty;
    public string State { get; init; } = string.Empty;
    public string PostalCode { get; init; } = string.Empty;
    public string Country { get; init; } = string.Empty;
    public bool IsDefault { get; init; }
    public string AddressType { get; init; } = "Shipping"; // Shipping, Billing
}

/// <summary>
/// Create/update address request.
/// </summary>
public record AddressRequest
{
    [Required]
    [StringLength(200)]
    public string Street { get; init; } = string.Empty;

    [Required]
    [StringLength(100)]
    public string City { get; init; } = string.Empty;

    [Required]
    [StringLength(100)]
    public string State { get; init; } = string.Empty;

    [Required]
    [StringLength(20)]
    public string PostalCode { get; init; } = string.Empty;

    [Required]
    [StringLength(100)]
    public string Country { get; init; } = string.Empty;

    public bool IsDefault { get; init; }
    public string AddressType { get; init; } = "Shipping";
}
