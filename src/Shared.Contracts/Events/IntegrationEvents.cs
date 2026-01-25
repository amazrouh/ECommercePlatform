namespace Shared.Contracts.Events;

/// <summary>
/// Base class for all integration events.
/// </summary>
public abstract record IntegrationEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();
    public DateTimeOffset OccurredAt { get; init; } = DateTimeOffset.UtcNow;
    public string EventType => GetType().Name;
}

// ==================== User Events ====================

/// <summary>
/// Published when a new user registers.
/// </summary>
public record UserRegisteredEvent : IntegrationEvent
{
    public Guid UserId { get; init; }
    public string Email { get; init; } = string.Empty;
    public string FirstName { get; init; } = string.Empty;
    public string LastName { get; init; } = string.Empty;
}

/// <summary>
/// Published when user profile is updated.
/// </summary>
public record UserProfileUpdatedEvent : IntegrationEvent
{
    public Guid UserId { get; init; }
    public string Email { get; init; } = string.Empty;
    public string FirstName { get; init; } = string.Empty;
    public string LastName { get; init; } = string.Empty;
}

// ==================== Product Events ====================

/// <summary>
/// Published when a product is created.
/// </summary>
public record ProductCreatedEvent : IntegrationEvent
{
    public Guid ProductId { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Sku { get; init; } = string.Empty;
    public decimal Price { get; init; }
    public int StockQuantity { get; init; }
}

/// <summary>
/// Published when product details are updated.
/// </summary>
public record ProductUpdatedEvent : IntegrationEvent
{
    public Guid ProductId { get; init; }
    public string Name { get; init; } = string.Empty;
    public decimal Price { get; init; }
    public decimal? SalePrice { get; init; }
    public int StockQuantity { get; init; }
    public bool IsActive { get; init; }
}

/// <summary>
/// Published when product stock changes.
/// </summary>
public record ProductStockChangedEvent : IntegrationEvent
{
    public Guid ProductId { get; init; }
    public string Sku { get; init; } = string.Empty;
    public int PreviousQuantity { get; init; }
    public int NewQuantity { get; init; }
    public string Reason { get; init; } = string.Empty; // Sale, Restock, Adjustment
}

/// <summary>
/// Published when stock is low.
/// </summary>
public record LowStockAlertEvent : IntegrationEvent
{
    public Guid ProductId { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Sku { get; init; } = string.Empty;
    public int CurrentQuantity { get; init; }
    public int ThresholdQuantity { get; init; }
}

// ==================== Cart Events ====================

/// <summary>
/// Published when cart is updated.
/// </summary>
public record CartUpdatedEvent : IntegrationEvent
{
    public Guid CartId { get; init; }
    public Guid UserId { get; init; }
    public int ItemCount { get; init; }
    public decimal Total { get; init; }
}

/// <summary>
/// Published when cart is abandoned (not checked out).
/// </summary>
public record CartAbandonedEvent : IntegrationEvent
{
    public Guid CartId { get; init; }
    public Guid UserId { get; init; }
    public string UserEmail { get; init; } = string.Empty;
    public decimal Total { get; init; }
    public DateTimeOffset LastUpdated { get; init; }
}

// ==================== Order Events ====================

/// <summary>
/// Published when an order is created.
/// </summary>
public record OrderCreatedEvent : IntegrationEvent
{
    public Guid OrderId { get; init; }
    public string OrderNumber { get; init; } = string.Empty;
    public Guid UserId { get; init; }
    public string UserEmail { get; init; } = string.Empty;
    public decimal Total { get; init; }
    public List<OrderItemInfo> Items { get; init; } = new();
}

/// <summary>
/// Order item info for events.
/// </summary>
public record OrderItemInfo
{
    public Guid ProductId { get; init; }
    public string ProductName { get; init; } = string.Empty;
    public string Sku { get; init; } = string.Empty;
    public int Quantity { get; init; }
    public decimal UnitPrice { get; init; }
}

/// <summary>
/// Published when order status changes.
/// </summary>
public record OrderStatusChangedEvent : IntegrationEvent
{
    public Guid OrderId { get; init; }
    public string OrderNumber { get; init; } = string.Empty;
    public Guid UserId { get; init; }
    public string UserEmail { get; init; } = string.Empty;
    public string PreviousStatus { get; init; } = string.Empty;
    public string NewStatus { get; init; } = string.Empty;
}

/// <summary>
/// Published when order is shipped.
/// </summary>
public record OrderShippedEvent : IntegrationEvent
{
    public Guid OrderId { get; init; }
    public string OrderNumber { get; init; } = string.Empty;
    public Guid UserId { get; init; }
    public string UserEmail { get; init; } = string.Empty;
    public string Carrier { get; init; } = string.Empty;
    public string TrackingNumber { get; init; } = string.Empty;
    public string? TrackingUrl { get; init; }
    public DateTimeOffset? EstimatedDelivery { get; init; }
}

/// <summary>
/// Published when order is delivered.
/// </summary>
public record OrderDeliveredEvent : IntegrationEvent
{
    public Guid OrderId { get; init; }
    public string OrderNumber { get; init; } = string.Empty;
    public Guid UserId { get; init; }
    public string UserEmail { get; init; } = string.Empty;
    public DateTimeOffset DeliveredAt { get; init; }
}

/// <summary>
/// Published when order is cancelled.
/// </summary>
public record OrderCancelledEvent : IntegrationEvent
{
    public Guid OrderId { get; init; }
    public string OrderNumber { get; init; } = string.Empty;
    public Guid UserId { get; init; }
    public string UserEmail { get; init; } = string.Empty;
    public string Reason { get; init; } = string.Empty;
    public List<OrderItemInfo> Items { get; init; } = new();
}

// ==================== Payment Events ====================

/// <summary>
/// Published when payment is completed.
/// </summary>
public record PaymentCompletedEvent : IntegrationEvent
{
    public Guid OrderId { get; init; }
    public string OrderNumber { get; init; } = string.Empty;
    public string TransactionId { get; init; } = string.Empty;
    public decimal Amount { get; init; }
    public string PaymentMethod { get; init; } = string.Empty;
}

/// <summary>
/// Published when payment fails.
/// </summary>
public record PaymentFailedEvent : IntegrationEvent
{
    public Guid OrderId { get; init; }
    public string OrderNumber { get; init; } = string.Empty;
    public Guid UserId { get; init; }
    public string UserEmail { get; init; } = string.Empty;
    public string Reason { get; init; } = string.Empty;
}
