using Shared.Contracts.DTOs;

namespace CartService.Services;

/// <summary>
/// Cart service interface.
/// </summary>
public interface ICartService
{
    Task<CartDto?> GetCartAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<CartSummaryDto> GetCartSummaryAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<CartDto> AddToCartAsync(Guid userId, AddToCartRequest request, CancellationToken cancellationToken = default);
    Task<CartDto?> UpdateCartItemAsync(Guid userId, Guid itemId, UpdateCartItemRequest request, CancellationToken cancellationToken = default);
    Task<bool> RemoveFromCartAsync(Guid userId, Guid itemId, CancellationToken cancellationToken = default);
    Task<CartDto?> ClearCartAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<CartDto?> ApplyCouponAsync(Guid userId, ApplyCouponRequest request, CancellationToken cancellationToken = default);
    Task<CartDto?> RemoveCouponAsync(Guid userId, CancellationToken cancellationToken = default);
}
