namespace CartService.Domain;

/// <summary>
/// Shopping cart entity.
/// </summary>
public class Cart
{
    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public string? CouponCode { get; private set; }
    public decimal Discount { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    private readonly List<CartItem> _items = new();
    public IReadOnlyCollection<CartItem> Items => _items.AsReadOnly();

    public decimal Subtotal => _items.Sum(i => i.LineTotal);
    public decimal Tax => Subtotal * 0.08m; // 8% tax
    public decimal ShippingCost => Subtotal > 100 ? 0 : 9.99m; // Free shipping over $100
    public decimal Total => Subtotal + Tax + ShippingCost - Discount;
    public int ItemCount => _items.Sum(i => i.Quantity);

    private Cart() { } // EF Core

    public static Cart Create(Guid userId)
    {
        return new Cart
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };
    }

    public CartItem AddItem(Guid productId, string productName, string? productThumbnail, string sku, decimal unitPrice, decimal? salePrice, int quantity, int availableStock)
    {
        var existingItem = _items.FirstOrDefault(i => i.ProductId == productId);

        if (existingItem != null)
        {
            existingItem.UpdateQuantity(existingItem.Quantity + quantity, availableStock);
            UpdatedAt = DateTimeOffset.UtcNow;
            return existingItem;
        }

        var item = CartItem.Create(Id, productId, productName, productThumbnail, sku, unitPrice, salePrice, quantity, availableStock);
        _items.Add(item);
        UpdatedAt = DateTimeOffset.UtcNow;
        return item;
    }

    public bool UpdateItemQuantity(Guid itemId, int quantity, int availableStock)
    {
        var item = _items.FirstOrDefault(i => i.Id == itemId);
        if (item == null) return false;

        if (quantity <= 0)
        {
            _items.Remove(item);
        }
        else
        {
            item.UpdateQuantity(quantity, availableStock);
        }

        UpdatedAt = DateTimeOffset.UtcNow;
        return true;
    }

    public bool RemoveItem(Guid itemId)
    {
        var item = _items.FirstOrDefault(i => i.Id == itemId);
        if (item == null) return false;

        _items.Remove(item);
        UpdatedAt = DateTimeOffset.UtcNow;
        return true;
    }

    public void Clear()
    {
        _items.Clear();
        CouponCode = null;
        Discount = 0;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public bool ApplyCoupon(string couponCode, decimal discountAmount)
    {
        CouponCode = couponCode;
        Discount = discountAmount;
        UpdatedAt = DateTimeOffset.UtcNow;
        return true;
    }

    public void RemoveCoupon()
    {
        CouponCode = null;
        Discount = 0;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void RefreshItem(Guid productId, decimal newPrice, decimal? newSalePrice, int newStock, bool isAvailable)
    {
        var item = _items.FirstOrDefault(i => i.ProductId == productId);
        if (item != null)
        {
            item.RefreshProductInfo(newPrice, newSalePrice, newStock, isAvailable);
            UpdatedAt = DateTimeOffset.UtcNow;
        }
    }
}

/// <summary>
/// Shopping cart item entity.
/// </summary>
public class CartItem
{
    public Guid Id { get; private set; }
    public Guid CartId { get; private set; }
    public Guid ProductId { get; private set; }
    public string ProductName { get; private set; } = string.Empty;
    public string? ProductThumbnail { get; private set; }
    public string Sku { get; private set; } = string.Empty;
    public decimal UnitPrice { get; private set; }
    public decimal? SalePrice { get; private set; }
    public int Quantity { get; private set; }
    public int AvailableStock { get; private set; }
    public bool IsAvailable { get; private set; } = true;
    public DateTimeOffset AddedAt { get; private set; }

    public decimal EffectivePrice => SalePrice ?? UnitPrice;
    public decimal LineTotal => EffectivePrice * Quantity;

    private CartItem() { } // EF Core

    public static CartItem Create(Guid cartId, Guid productId, string productName, string? productThumbnail, string sku, decimal unitPrice, decimal? salePrice, int quantity, int availableStock)
    {
        return new CartItem
        {
            Id = Guid.NewGuid(),
            CartId = cartId,
            ProductId = productId,
            ProductName = productName,
            ProductThumbnail = productThumbnail,
            Sku = sku,
            UnitPrice = unitPrice,
            SalePrice = salePrice,
            Quantity = Math.Min(quantity, availableStock),
            AvailableStock = availableStock,
            IsAvailable = availableStock > 0,
            AddedAt = DateTimeOffset.UtcNow
        };
    }

    public void UpdateQuantity(int quantity, int availableStock)
    {
        AvailableStock = availableStock;
        Quantity = Math.Min(quantity, availableStock);
        IsAvailable = availableStock > 0;
    }

    public void RefreshProductInfo(decimal newPrice, decimal? newSalePrice, int newStock, bool isAvailable)
    {
        UnitPrice = newPrice;
        SalePrice = newSalePrice;
        AvailableStock = newStock;
        IsAvailable = isAvailable;
        if (Quantity > newStock && newStock > 0)
        {
            Quantity = newStock;
        }
    }
}
