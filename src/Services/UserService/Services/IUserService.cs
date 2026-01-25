using Shared.Contracts.DTOs;

namespace UserService.Services;

/// <summary>
/// User management service interface.
/// </summary>
public interface IUserService
{
    Task<UserDto?> GetByIdAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<UserDto?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);
    Task<UserDto?> UpdateProfileAsync(Guid userId, UpdateProfileRequest request, CancellationToken cancellationToken = default);
    Task<bool> DeactivateAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<IEnumerable<AddressDto>> GetAddressesAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<AddressDto?> AddAddressAsync(Guid userId, AddressRequest request, CancellationToken cancellationToken = default);
    Task<AddressDto?> UpdateAddressAsync(Guid userId, Guid addressId, AddressRequest request, CancellationToken cancellationToken = default);
    Task<bool> DeleteAddressAsync(Guid userId, Guid addressId, CancellationToken cancellationToken = default);
}
