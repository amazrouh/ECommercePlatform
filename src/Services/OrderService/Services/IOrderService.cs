using Shared.Contracts.DTOs;

namespace OrderService.Services;

/// <summary>
/// Order service interface.
/// </summary>
public interface IOrderService
{
    Task<OrderDto> CreateOrderAsync(Guid userId, string userEmail, string userName, CreateOrderRequest request, CancellationToken cancellationToken = default);
    Task<OrderDto?> GetOrderByIdAsync(Guid orderId, CancellationToken cancellationToken = default);
    Task<OrderDto?> GetOrderByNumberAsync(string orderNumber, CancellationToken cancellationToken = default);
    Task<PagedResult<OrderSummaryDto>> GetUserOrdersAsync(Guid userId, int page, int pageSize, CancellationToken cancellationToken = default);
    Task<OrderDto?> ConfirmPaymentAsync(Guid orderId, string transactionId, CancellationToken cancellationToken = default);
    Task<OrderDto?> ProcessOrderAsync(Guid orderId, CancellationToken cancellationToken = default);
    Task<OrderDto?> ShipOrderAsync(Guid orderId, string carrier, string trackingNumber, string? trackingUrl, CancellationToken cancellationToken = default);
    Task<OrderDto?> DeliverOrderAsync(Guid orderId, CancellationToken cancellationToken = default);
    Task<OrderDto?> CompleteOrderAsync(Guid orderId, CancellationToken cancellationToken = default);
    Task<OrderDto?> CancelOrderAsync(Guid orderId, string reason, CancellationToken cancellationToken = default);
}
