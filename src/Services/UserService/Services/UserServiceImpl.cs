using Microsoft.EntityFrameworkCore;
using Shared.Contracts.DTOs;
using UserService.Data;

namespace UserService.Services;

/// <summary>
/// User management service implementation.
/// </summary>
public class UserServiceImpl : IUserService
{
    private readonly UserDbContext _context;
    private readonly ILogger<UserServiceImpl> _logger;

    public UserServiceImpl(UserDbContext context, ILogger<UserServiceImpl> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<UserDto?> GetByIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await _context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);

        return user == null ? null : MapToDto(user);
    }

    public async Task<UserDto?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        var user = await _context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Email == email.ToLowerInvariant(), cancellationToken);

        return user == null ? null : MapToDto(user);
    }

    public async Task<UserDto?> UpdateProfileAsync(Guid userId, UpdateProfileRequest request, CancellationToken cancellationToken = default)
    {
        var user = await _context.Users.FindAsync(new object[] { userId }, cancellationToken);
        if (user == null) return null;

        user.UpdateProfile(request.FirstName, request.LastName, request.PhoneNumber);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Profile updated for user: {UserId}", userId);
        return MapToDto(user);
    }

    public async Task<bool> DeactivateAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await _context.Users.FindAsync(new object[] { userId }, cancellationToken);
        if (user == null) return false;

        user.Deactivate();
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("User deactivated: {UserId}", userId);
        return true;
    }

    public async Task<IEnumerable<AddressDto>> GetAddressesAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var addresses = await _context.Addresses
            .AsNoTracking()
            .Where(a => a.UserId == userId)
            .ToListAsync(cancellationToken);

        return addresses.Select(MapToAddressDto);
    }

    public async Task<AddressDto?> AddAddressAsync(Guid userId, AddressRequest request, CancellationToken cancellationToken = default)
    {
        var user = await _context.Users
            .Include(u => u.Addresses)
            .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);

        if (user == null) return null;

        var address = user.AddAddress(
            request.Street, request.City, request.State,
            request.PostalCode, request.Country, request.AddressType, request.IsDefault);

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Address added for user: {UserId}", userId);
        return MapToAddressDto(address);
    }

    public async Task<AddressDto?> UpdateAddressAsync(Guid userId, Guid addressId, AddressRequest request, CancellationToken cancellationToken = default)
    {
        var address = await _context.Addresses
            .FirstOrDefaultAsync(a => a.Id == addressId && a.UserId == userId, cancellationToken);

        if (address == null) return null;

        address.Update(request.Street, request.City, request.State, request.PostalCode, request.Country);

        if (request.IsDefault)
        {
            var otherAddresses = await _context.Addresses
                .Where(a => a.UserId == userId && a.Id != addressId && a.AddressType == request.AddressType)
                .ToListAsync(cancellationToken);

            foreach (var addr in otherAddresses)
            {
                addr.SetDefault(false);
            }
            address.SetDefault(true);
        }

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Address updated: {AddressId} for user: {UserId}", addressId, userId);
        return MapToAddressDto(address);
    }

    public async Task<bool> DeleteAddressAsync(Guid userId, Guid addressId, CancellationToken cancellationToken = default)
    {
        var address = await _context.Addresses
            .FirstOrDefaultAsync(a => a.Id == addressId && a.UserId == userId, cancellationToken);

        if (address == null) return false;

        _context.Addresses.Remove(address);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Address deleted: {AddressId} for user: {UserId}", addressId, userId);
        return true;
    }

    private static UserDto MapToDto(Domain.User user) => new()
    {
        Id = user.Id,
        Email = user.Email,
        FirstName = user.FirstName,
        LastName = user.LastName,
        PhoneNumber = user.PhoneNumber,
        Role = user.Role,
        CreatedAt = user.CreatedAt,
        IsActive = user.IsActive
    };

    private static AddressDto MapToAddressDto(Domain.Address address) => new()
    {
        Id = address.Id,
        Street = address.Street,
        City = address.City,
        State = address.State,
        PostalCode = address.PostalCode,
        Country = address.Country,
        AddressType = address.AddressType,
        IsDefault = address.IsDefault
    };
}
