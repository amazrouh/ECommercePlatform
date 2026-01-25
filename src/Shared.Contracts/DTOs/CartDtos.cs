using System.ComponentModel.DataAnnotations;

namespace Shared.Contracts.DTOs;

/// <summary>
/// Shopping cart data transfer object.
/// </summary>
public record CartDto
{
    public Guid Id { get; init; }
    public Guid UserId { get; init; }
    public List<CartItemDto> Items { get; init; } = new();
    public decimal Subtotal => Items.Sum(i => i.LineTotal);
    public decimal Tax { get; init; }
    public decimal ShippingCost { get; init; }
    public decimal Discount { get; init; }
    public decimal Total => Subtotal + Tax + ShippingCost - Discount;
    public int ItemCount => Items.Sum(i => i.Quantity);
    public string? CouponCode { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset UpdatedAt { get; init; }
}

/// <summary>
/// Cart item data transfer object.
/// </summary>
public record CartItemDto
{
    public Guid Id { get; init; }
    public Guid ProductId { get; init; }
    public string ProductName { get; init; } = string.Empty;
    public string? ProductThumbnail { get; init; }
    public string Sku { get; init; } = string.Empty;
    public decimal UnitPrice { get; init; }
    public decimal? SalePrice { get; init; }
    public decimal EffectivePrice => SalePrice ?? UnitPrice;
    public int Quantity { get; init; }
    public decimal LineTotal => EffectivePrice * Quantity;
    public int AvailableStock { get; init; }
    public bool IsAvailable { get; init; } = true;
}

/// <summary>
/// Add item to cart request.
/// </summary>
public record AddToCartRequest
{
    [Required]
    public Guid ProductId { get; init; }

    [Required]
    [Range(1, 100)]
    public int Quantity { get; init; } = 1;
}

/// <summary>
/// Update cart item quantity request.
/// </summary>
public record UpdateCartItemRequest
{
    [Required]
    [Range(0, 100)]
    public int Quantity { get; init; }
}

/// <summary>
/// Apply coupon request.
/// </summary>
public record ApplyCouponRequest
{
    [Required]
    [StringLength(50)]
    public string CouponCode { get; init; } = string.Empty;
}

/// <summary>
/// Cart summary for header display.
/// </summary>
public record CartSummaryDto
{
    public int ItemCount { get; init; }
    public decimal Total { get; init; }
}
