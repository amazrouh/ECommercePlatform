using Shared.Contracts.DTOs;

namespace OrderService.Domain;

/// <summary>
/// Order aggregate root.
/// </summary>
public class Order
{
    public Guid Id { get; private set; }
    public string OrderNumber { get; private set; } = string.Empty;
    public Guid UserId { get; private set; }
    public string UserEmail { get; private set; } = string.Empty;
    public string UserName { get; private set; } = string.Empty;
    public OrderStatus Status { get; private set; }

    // Addresses (stored as JSON or separate entity)
    public string ShippingStreet { get; private set; } = string.Empty;
    public string ShippingCity { get; private set; } = string.Empty;
    public string ShippingState { get; private set; } = string.Empty;
    public string ShippingPostalCode { get; private set; } = string.Empty;
    public string ShippingCountry { get; private set; } = string.Empty;

    public string? BillingStreet { get; private set; }
    public string? BillingCity { get; private set; }
    public string? BillingState { get; private set; }
    public string? BillingPostalCode { get; private set; }
    public string? BillingCountry { get; private set; }

    public decimal Subtotal { get; private set; }
    public decimal Tax { get; private set; }
    public decimal ShippingCost { get; private set; }
    public decimal Discount { get; private set; }
    public decimal Total { get; private set; }
    public string? CouponCode { get; private set; }

    // Payment
    public string PaymentMethod { get; private set; } = string.Empty;
    public string? PaymentTransactionId { get; private set; }
    public PaymentStatus PaymentStatus { get; private set; }
    public DateTimeOffset? PaidAt { get; private set; }

    // Shipping
    public string? Carrier { get; private set; }
    public string? TrackingNumber { get; private set; }
    public string? TrackingUrl { get; private set; }
    public DateTimeOffset? ShippedAt { get; private set; }
    public DateTimeOffset? DeliveredAt { get; private set; }
    public DateTimeOffset? EstimatedDelivery { get; private set; }

    public string? Notes { get; private set; }
    public string? CancellationReason { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset? CompletedAt { get; private set; }

    private readonly List<OrderItem> _items = new();
    public IReadOnlyCollection<OrderItem> Items => _items.AsReadOnly();

    private Order() { } // EF Core

    public static Order Create(
        Guid userId,
        string userEmail,
        string userName,
        AddressRequest shippingAddress,
        AddressRequest? billingAddress,
        string paymentMethod,
        string? couponCode,
        decimal discount,
        string? notes)
    {
        var order = new Order
        {
            Id = Guid.NewGuid(),
            OrderNumber = GenerateOrderNumber(),
            UserId = userId,
            UserEmail = userEmail,
            UserName = userName,
            Status = OrderStatus.Pending,

            ShippingStreet = shippingAddress.Street,
            ShippingCity = shippingAddress.City,
            ShippingState = shippingAddress.State,
            ShippingPostalCode = shippingAddress.PostalCode,
            ShippingCountry = shippingAddress.Country,

            PaymentMethod = paymentMethod,
            PaymentStatus = PaymentStatus.Pending,
            CouponCode = couponCode,
            Discount = discount,
            Notes = notes,
            CreatedAt = DateTimeOffset.UtcNow
        };

        if (billingAddress != null)
        {
            order.BillingStreet = billingAddress.Street;
            order.BillingCity = billingAddress.City;
            order.BillingState = billingAddress.State;
            order.BillingPostalCode = billingAddress.PostalCode;
            order.BillingCountry = billingAddress.Country;
        }

        return order;
    }

    public void AddItem(Guid productId, string productName, string? productThumbnail, string sku, decimal unitPrice, int quantity)
    {
        var item = OrderItem.Create(Id, productId, productName, productThumbnail, sku, unitPrice, quantity);
        _items.Add(item);
        RecalculateTotals();
    }

    public void ConfirmPayment(string transactionId)
    {
        PaymentTransactionId = transactionId;
        PaymentStatus = PaymentStatus.Completed;
        PaidAt = DateTimeOffset.UtcNow;
        Status = OrderStatus.Confirmed;
    }

    public void MarkAsProcessing()
    {
        if (Status != OrderStatus.Confirmed)
            throw new InvalidOperationException("Order must be confirmed before processing");

        Status = OrderStatus.Processing;
    }

    public void Ship(string carrier, string trackingNumber, string? trackingUrl, DateTimeOffset? estimatedDelivery)
    {
        if (Status != OrderStatus.Processing)
            throw new InvalidOperationException("Order must be processing before shipping");

        Carrier = carrier;
        TrackingNumber = trackingNumber;
        TrackingUrl = trackingUrl;
        EstimatedDelivery = estimatedDelivery;
        ShippedAt = DateTimeOffset.UtcNow;
        Status = OrderStatus.Shipped;
    }

    public void MarkAsDelivered()
    {
        if (Status != OrderStatus.Shipped)
            throw new InvalidOperationException("Order must be shipped before delivery");

        DeliveredAt = DateTimeOffset.UtcNow;
        Status = OrderStatus.Delivered;
    }

    public void Complete()
    {
        if (Status != OrderStatus.Delivered)
            throw new InvalidOperationException("Order must be delivered before completion");

        Status = OrderStatus.Completed;
        CompletedAt = DateTimeOffset.UtcNow;
    }

    public void Cancel(string reason)
    {
        if (Status == OrderStatus.Shipped || Status == OrderStatus.Delivered || Status == OrderStatus.Completed)
            throw new InvalidOperationException("Cannot cancel shipped or completed orders");

        CancellationReason = reason;
        Status = OrderStatus.Cancelled;

        if (PaymentStatus == PaymentStatus.Completed)
        {
            PaymentStatus = PaymentStatus.Refunded;
        }
    }

    private void RecalculateTotals()
    {
        Subtotal = _items.Sum(i => i.LineTotal);
        Tax = Subtotal * 0.08m; // 8% tax
        ShippingCost = Subtotal > 100 ? 0 : 9.99m;
        Total = Subtotal + Tax + ShippingCost - Discount;
    }

    private static string GenerateOrderNumber()
    {
        var timestamp = DateTime.UtcNow.ToString("yyyyMMddHHmmss");
        var random = new Random().Next(1000, 9999);
        return $"ORD-{timestamp}-{random}";
    }
}

/// <summary>
/// Order item entity.
/// </summary>
public class OrderItem
{
    public Guid Id { get; private set; }
    public Guid OrderId { get; private set; }
    public Guid ProductId { get; private set; }
    public string ProductName { get; private set; } = string.Empty;
    public string? ProductThumbnail { get; private set; }
    public string Sku { get; private set; } = string.Empty;
    public decimal UnitPrice { get; private set; }
    public int Quantity { get; private set; }
    public decimal LineTotal => UnitPrice * Quantity;

    private OrderItem() { } // EF Core

    public static OrderItem Create(Guid orderId, Guid productId, string productName, string? productThumbnail, string sku, decimal unitPrice, int quantity)
    {
        return new OrderItem
        {
            Id = Guid.NewGuid(),
            OrderId = orderId,
            ProductId = productId,
            ProductName = productName,
            ProductThumbnail = productThumbnail,
            Sku = sku,
            UnitPrice = unitPrice,
            Quantity = quantity
        };
    }
}
