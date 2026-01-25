using System.ComponentModel.DataAnnotations;

namespace Shared.Contracts.DTOs;

/// <summary>
/// Order data transfer object.
/// </summary>
public record OrderDto
{
    public Guid Id { get; init; }
    public string OrderNumber { get; init; } = string.Empty;
    public Guid UserId { get; init; }
    public string UserEmail { get; init; } = string.Empty;
    public string UserName { get; init; } = string.Empty;
    public OrderStatus Status { get; init; }
    public string StatusDisplay => Status.ToString();
    public List<OrderItemDto> Items { get; init; } = new();
    public AddressDto ShippingAddress { get; init; } = null!;
    public AddressDto? BillingAddress { get; init; }
    public decimal Subtotal { get; init; }
    public decimal Tax { get; init; }
    public decimal ShippingCost { get; init; }
    public decimal Discount { get; init; }
    public decimal Total { get; init; }
    public string? CouponCode { get; init; }
    public PaymentInfo? Payment { get; init; }
    public ShippingInfo? Shipping { get; init; }
    public string? Notes { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset? CompletedAt { get; init; }
}

/// <summary>
/// Order item data transfer object.
/// </summary>
public record OrderItemDto
{
    public Guid Id { get; init; }
    public Guid ProductId { get; init; }
    public string ProductName { get; init; } = string.Empty;
    public string? ProductThumbnail { get; init; }
    public string Sku { get; init; } = string.Empty;
    public decimal UnitPrice { get; init; }
    public int Quantity { get; init; }
    public decimal LineTotal => UnitPrice * Quantity;
}

/// <summary>
/// Order summary for listings.
/// </summary>
public record OrderSummaryDto
{
    public Guid Id { get; init; }
    public string OrderNumber { get; init; } = string.Empty;
    public OrderStatus Status { get; init; }
    public string StatusDisplay => Status.ToString();
    public int ItemCount { get; init; }
    public decimal Total { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
}

/// <summary>
/// Create order request (checkout).
/// </summary>
public record CreateOrderRequest
{
    [Required]
    public AddressRequest ShippingAddress { get; init; } = null!;

    public AddressRequest? BillingAddress { get; init; }

    public bool BillingSameAsShipping { get; init; } = true;

    [Required]
    public PaymentRequest Payment { get; init; } = null!;

    [StringLength(500)]
    public string? Notes { get; init; }

    public string? CouponCode { get; init; }
}

/// <summary>
/// Payment request.
/// </summary>
public record PaymentRequest
{
    [Required]
    public string PaymentMethod { get; init; } = "CreditCard"; // CreditCard, PayPal, etc.

    public string? PaymentToken { get; init; } // Token from payment provider

    // For demo purposes - in production use payment provider tokens
    public string? CardNumber { get; init; }
    public string? CardHolderName { get; init; }
    public string? ExpiryMonth { get; init; }
    public string? ExpiryYear { get; init; }
    public string? Cvv { get; init; }
}

/// <summary>
/// Payment information.
/// </summary>
public record PaymentInfo
{
    public string PaymentMethod { get; init; } = string.Empty;
    public string? TransactionId { get; init; }
    public PaymentStatus Status { get; init; }
    public decimal Amount { get; init; }
    public DateTimeOffset? PaidAt { get; init; }
    public string? LastFourDigits { get; init; }
}

/// <summary>
/// Shipping information.
/// </summary>
public record ShippingInfo
{
    public string Carrier { get; init; } = string.Empty;
    public string? TrackingNumber { get; init; }
    public string? TrackingUrl { get; init; }
    public DateTimeOffset? ShippedAt { get; init; }
    public DateTimeOffset? DeliveredAt { get; init; }
    public DateTimeOffset? EstimatedDelivery { get; init; }
}

/// <summary>
/// Order status enumeration.
/// </summary>
public enum OrderStatus
{
    Pending = 0,
    Confirmed = 1,
    Processing = 2,
    Shipped = 3,
    Delivered = 4,
    Completed = 5,
    Cancelled = 6,
    Refunded = 7
}

/// <summary>
/// Payment status enumeration.
/// </summary>
public enum PaymentStatus
{
    Pending = 0,
    Processing = 1,
    Completed = 2,
    Failed = 3,
    Refunded = 4,
    Cancelled = 5
}

/// <summary>
/// Cancel order request.
/// </summary>
public record CancelOrderRequest
{
    [Required]
    [StringLength(500)]
    public string Reason { get; init; } = string.Empty;
}
