using Microsoft.EntityFrameworkCore;
using OrderService.Data;
using OrderService.Domain;
using Shared.Contracts.DTOs;

namespace OrderService.Services;

/// <summary>
/// Order service implementation.
/// </summary>
public class OrderServiceImpl : IOrderService
{
    private readonly OrderDbContext _context;
    private readonly ILogger<OrderServiceImpl> _logger;

    // Demo cart data (in production, this would come from Cart Service)
    private static readonly Dictionary<Guid, List<(Guid ProductId, string Name, string? Thumbnail, string Sku, decimal Price, int Quantity)>> _demoCartItems = new()
    {
        {
            Guid.Parse("22222222-2222-2222-2222-222222222222"),
            new List<(Guid, string, string?, string, decimal, int)>
            {
                (Guid.Parse("11111111-aaaa-aaaa-aaaa-111111111111"), "Wireless Bluetooth Headphones", "https://example.com/headphones1.jpg", "ELEC-HP-001", 129.99m, 1),
                (Guid.Parse("44444444-aaaa-aaaa-aaaa-444444444444"), "Clean Code: A Handbook", "https://example.com/cleancode.jpg", "BOOK-DEV-001", 44.99m, 2)
            }
        }
    };

    public OrderServiceImpl(OrderDbContext context, ILogger<OrderServiceImpl> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<OrderDto> CreateOrderAsync(Guid userId, string userEmail, string userName, CreateOrderRequest request, CancellationToken cancellationToken = default)
    {
        var billingAddress = request.BillingSameAsShipping ? null : request.BillingAddress;

        var order = Order.Create(
            userId,
            userEmail,
            userName,
            request.ShippingAddress,
            billingAddress,
            request.Payment.PaymentMethod,
            request.CouponCode,
            0, // Discount will be calculated
            request.Notes
        );

        // Get cart items (demo data - in production, call Cart Service)
        var cartItems = _demoCartItems.GetValueOrDefault(userId) ?? new List<(Guid, string, string?, string, decimal, int)>
        {
            (Guid.Parse("22222222-aaaa-aaaa-aaaa-222222222222"), "Smart Watch Pro", "https://example.com/smartwatch1.jpg", "ELEC-SW-001", 299.99m, 1)
        };

        foreach (var item in cartItems)
        {
            order.AddItem(item.ProductId, item.Name, item.Thumbnail, item.Sku, item.Price, item.Quantity);
        }

        // Simulate payment processing
        order.ConfirmPayment($"TXN-{Guid.NewGuid():N}".Substring(0, 20));

        _context.Orders.Add(order);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Order created: {OrderNumber} for user {UserId}", order.OrderNumber, userId);

        return MapToDto(order);
    }

    public async Task<OrderDto?> GetOrderByIdAsync(Guid orderId, CancellationToken cancellationToken = default)
    {
        var order = await _context.Orders
            .Include(o => o.Items)
            .AsNoTracking()
            .FirstOrDefaultAsync(o => o.Id == orderId, cancellationToken);

        return order == null ? null : MapToDto(order);
    }

    public async Task<OrderDto?> GetOrderByNumberAsync(string orderNumber, CancellationToken cancellationToken = default)
    {
        var order = await _context.Orders
            .Include(o => o.Items)
            .AsNoTracking()
            .FirstOrDefaultAsync(o => o.OrderNumber == orderNumber, cancellationToken);

        return order == null ? null : MapToDto(order);
    }

    public async Task<PagedResult<OrderSummaryDto>> GetUserOrdersAsync(Guid userId, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var query = _context.Orders
            .Include(o => o.Items)
            .Where(o => o.UserId == userId)
            .OrderByDescending(o => o.CreatedAt);

        var totalCount = await query.CountAsync(cancellationToken);

        var orders = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(o => new OrderSummaryDto
            {
                Id = o.Id,
                OrderNumber = o.OrderNumber,
                Status = o.Status,
                ItemCount = o.Items.Count,
                Total = o.Total,
                CreatedAt = o.CreatedAt
            })
            .ToListAsync(cancellationToken);

        return new PagedResult<OrderSummaryDto>
        {
            Items = orders,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<OrderDto?> ConfirmPaymentAsync(Guid orderId, string transactionId, CancellationToken cancellationToken = default)
    {
        var order = await GetOrderWithItemsAsync(orderId, cancellationToken);
        if (order == null) return null;

        order.ConfirmPayment(transactionId);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Payment confirmed for order: {OrderNumber}", order.OrderNumber);
        return MapToDto(order);
    }

    public async Task<OrderDto?> ProcessOrderAsync(Guid orderId, CancellationToken cancellationToken = default)
    {
        var order = await GetOrderWithItemsAsync(orderId, cancellationToken);
        if (order == null) return null;

        order.MarkAsProcessing();
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Order processing: {OrderNumber}", order.OrderNumber);
        return MapToDto(order);
    }

    public async Task<OrderDto?> ShipOrderAsync(Guid orderId, string carrier, string trackingNumber, string? trackingUrl, CancellationToken cancellationToken = default)
    {
        var order = await GetOrderWithItemsAsync(orderId, cancellationToken);
        if (order == null) return null;

        var estimatedDelivery = DateTimeOffset.UtcNow.AddDays(5);
        order.Ship(carrier, trackingNumber, trackingUrl, estimatedDelivery);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Order shipped: {OrderNumber}, Tracking: {TrackingNumber}", order.OrderNumber, trackingNumber);
        return MapToDto(order);
    }

    public async Task<OrderDto?> DeliverOrderAsync(Guid orderId, CancellationToken cancellationToken = default)
    {
        var order = await GetOrderWithItemsAsync(orderId, cancellationToken);
        if (order == null) return null;

        order.MarkAsDelivered();
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Order delivered: {OrderNumber}", order.OrderNumber);
        return MapToDto(order);
    }

    public async Task<OrderDto?> CompleteOrderAsync(Guid orderId, CancellationToken cancellationToken = default)
    {
        var order = await GetOrderWithItemsAsync(orderId, cancellationToken);
        if (order == null) return null;

        order.Complete();
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Order completed: {OrderNumber}", order.OrderNumber);
        return MapToDto(order);
    }

    public async Task<OrderDto?> CancelOrderAsync(Guid orderId, string reason, CancellationToken cancellationToken = default)
    {
        var order = await GetOrderWithItemsAsync(orderId, cancellationToken);
        if (order == null) return null;

        order.Cancel(reason);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Order cancelled: {OrderNumber}, Reason: {Reason}", order.OrderNumber, reason);
        return MapToDto(order);
    }

    private async Task<Order?> GetOrderWithItemsAsync(Guid orderId, CancellationToken cancellationToken)
    {
        return await _context.Orders
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.Id == orderId, cancellationToken);
    }

    private static OrderDto MapToDto(Order order) => new()
    {
        Id = order.Id,
        OrderNumber = order.OrderNumber,
        UserId = order.UserId,
        UserEmail = order.UserEmail,
        UserName = order.UserName,
        Status = order.Status,
        Items = order.Items.Select(i => new OrderItemDto
        {
            Id = i.Id,
            ProductId = i.ProductId,
            ProductName = i.ProductName,
            ProductThumbnail = i.ProductThumbnail,
            Sku = i.Sku,
            UnitPrice = i.UnitPrice,
            Quantity = i.Quantity
        }).ToList(),
        ShippingAddress = new AddressDto
        {
            Street = order.ShippingStreet,
            City = order.ShippingCity,
            State = order.ShippingState,
            PostalCode = order.ShippingPostalCode,
            Country = order.ShippingCountry,
            AddressType = "Shipping",
            IsDefault = true
        },
        BillingAddress = order.BillingStreet != null ? new AddressDto
        {
            Street = order.BillingStreet,
            City = order.BillingCity!,
            State = order.BillingState!,
            PostalCode = order.BillingPostalCode!,
            Country = order.BillingCountry!,
            AddressType = "Billing",
            IsDefault = false
        } : null,
        Subtotal = order.Subtotal,
        Tax = order.Tax,
        ShippingCost = order.ShippingCost,
        Discount = order.Discount,
        Total = order.Total,
        CouponCode = order.CouponCode,
        Payment = new PaymentInfo
        {
            PaymentMethod = order.PaymentMethod,
            TransactionId = order.PaymentTransactionId,
            Status = order.PaymentStatus,
            Amount = order.Total,
            PaidAt = order.PaidAt
        },
        Shipping = order.ShippedAt.HasValue ? new ShippingInfo
        {
            Carrier = order.Carrier!,
            TrackingNumber = order.TrackingNumber,
            TrackingUrl = order.TrackingUrl,
            ShippedAt = order.ShippedAt,
            DeliveredAt = order.DeliveredAt,
            EstimatedDelivery = order.EstimatedDelivery
        } : null,
        Notes = order.Notes,
        CreatedAt = order.CreatedAt,
        CompletedAt = order.CompletedAt
    };
}
