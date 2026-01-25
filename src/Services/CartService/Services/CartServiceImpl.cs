using CartService.Data;
using CartService.Domain;
using Microsoft.EntityFrameworkCore;
using Shared.Contracts.DTOs;

namespace CartService.Services;

/// <summary>
/// Cart service implementation.
/// </summary>
public class CartServiceImpl : ICartService
{
    private readonly CartDbContext _context;
    private readonly ILogger<CartServiceImpl> _logger;
    private readonly HttpClient _productClient;

    // Demo product data (in production, this would come from Product Service via HTTP)
    private static readonly Dictionary<Guid, (string Name, string? Thumbnail, string Sku, decimal Price, decimal? SalePrice, int Stock)> _demoProducts = new()
    {
        { Guid.Parse("11111111-aaaa-aaaa-aaaa-111111111111"), ("Wireless Bluetooth Headphones", "https://example.com/headphones1.jpg", "ELEC-HP-001", 149.99m, 129.99m, 50) },
        { Guid.Parse("22222222-aaaa-aaaa-aaaa-222222222222"), ("Smart Watch Pro", "https://example.com/smartwatch1.jpg", "ELEC-SW-001", 299.99m, null, 30) },
        { Guid.Parse("33333333-aaaa-aaaa-aaaa-333333333333"), ("Classic Cotton T-Shirt", "https://example.com/tshirt1.jpg", "CLTH-TS-001", 29.99m, null, 200) },
        { Guid.Parse("44444444-aaaa-aaaa-aaaa-444444444444"), ("Clean Code: A Handbook", "https://example.com/cleancode.jpg", "BOOK-DEV-001", 44.99m, null, 75) }
    };

    public CartServiceImpl(CartDbContext context, ILogger<CartServiceImpl> logger, IHttpClientFactory httpClientFactory)
    {
        _context = context;
        _logger = logger;
        _productClient = httpClientFactory.CreateClient("ProductService");
    }

    public async Task<CartDto?> GetCartAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var cart = await GetOrCreateCartAsync(userId, cancellationToken);
        return MapToDto(cart);
    }

    public async Task<CartSummaryDto> GetCartSummaryAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var cart = await _context.Carts
            .Include(c => c.Items)
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.UserId == userId, cancellationToken);

        return new CartSummaryDto
        {
            ItemCount = cart?.ItemCount ?? 0,
            Total = cart?.Total ?? 0
        };
    }

    public async Task<CartDto> AddToCartAsync(Guid userId, AddToCartRequest request, CancellationToken cancellationToken = default)
    {
        var cart = await GetOrCreateCartAsync(userId, cancellationToken);

        // Get product info (demo data - in production, call Product Service)
        if (!_demoProducts.TryGetValue(request.ProductId, out var product))
        {
            throw new InvalidOperationException($"Product {request.ProductId} not found");
        }

        cart.AddItem(
            request.ProductId,
            product.Name,
            product.Thumbnail,
            product.Sku,
            product.Price,
            product.SalePrice,
            request.Quantity,
            product.Stock
        );

        await _context.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Added product {ProductId} to cart for user {UserId}", request.ProductId, userId);

        return MapToDto(cart);
    }

    public async Task<CartDto?> UpdateCartItemAsync(Guid userId, Guid itemId, UpdateCartItemRequest request, CancellationToken cancellationToken = default)
    {
        var cart = await _context.Carts
            .Include(c => c.Items)
            .FirstOrDefaultAsync(c => c.UserId == userId, cancellationToken);

        if (cart == null) return null;

        var item = cart.Items.FirstOrDefault(i => i.Id == itemId);
        if (item == null) return null;

        if (request.Quantity <= 0)
        {
            cart.RemoveItem(itemId);
        }
        else
        {
            cart.UpdateItemQuantity(itemId, request.Quantity, item.AvailableStock);
        }

        await _context.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Updated cart item {ItemId} for user {UserId}", itemId, userId);

        return MapToDto(cart);
    }

    public async Task<bool> RemoveFromCartAsync(Guid userId, Guid itemId, CancellationToken cancellationToken = default)
    {
        var cart = await _context.Carts
            .Include(c => c.Items)
            .FirstOrDefaultAsync(c => c.UserId == userId, cancellationToken);

        if (cart == null) return false;

        var success = cart.RemoveItem(itemId);
        if (success)
        {
            await _context.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Removed item {ItemId} from cart for user {UserId}", itemId, userId);
        }

        return success;
    }

    public async Task<CartDto?> ClearCartAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var cart = await _context.Carts
            .Include(c => c.Items)
            .FirstOrDefaultAsync(c => c.UserId == userId, cancellationToken);

        if (cart == null) return null;

        cart.Clear();
        await _context.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Cleared cart for user {UserId}", userId);

        return MapToDto(cart);
    }

    public async Task<CartDto?> ApplyCouponAsync(Guid userId, ApplyCouponRequest request, CancellationToken cancellationToken = default)
    {
        var cart = await _context.Carts
            .Include(c => c.Items)
            .FirstOrDefaultAsync(c => c.UserId == userId, cancellationToken);

        if (cart == null) return null;

        // Demo coupon validation
        var discount = request.CouponCode.ToUpperInvariant() switch
        {
            "SAVE10" => cart.Subtotal * 0.10m,
            "SAVE20" => cart.Subtotal * 0.20m,
            "FLAT15" => 15m,
            _ => 0m
        };

        if (discount <= 0)
        {
            throw new InvalidOperationException("Invalid coupon code");
        }

        cart.ApplyCoupon(request.CouponCode.ToUpperInvariant(), discount);
        await _context.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Applied coupon {CouponCode} for user {UserId}", request.CouponCode, userId);

        return MapToDto(cart);
    }

    public async Task<CartDto?> RemoveCouponAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var cart = await _context.Carts
            .Include(c => c.Items)
            .FirstOrDefaultAsync(c => c.UserId == userId, cancellationToken);

        if (cart == null) return null;

        cart.RemoveCoupon();
        await _context.SaveChangesAsync(cancellationToken);

        return MapToDto(cart);
    }

    private async Task<Cart> GetOrCreateCartAsync(Guid userId, CancellationToken cancellationToken)
    {
        var cart = await _context.Carts
            .Include(c => c.Items)
            .FirstOrDefaultAsync(c => c.UserId == userId, cancellationToken);

        if (cart == null)
        {
            cart = Cart.Create(userId);
            _context.Carts.Add(cart);
            await _context.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Created new cart for user {UserId}", userId);
        }

        return cart;
    }

    private static CartDto MapToDto(Cart cart) => new()
    {
        Id = cart.Id,
        UserId = cart.UserId,
        Items = cart.Items.Select(i => new CartItemDto
        {
            Id = i.Id,
            ProductId = i.ProductId,
            ProductName = i.ProductName,
            ProductThumbnail = i.ProductThumbnail,
            Sku = i.Sku,
            UnitPrice = i.UnitPrice,
            SalePrice = i.SalePrice,
            Quantity = i.Quantity,
            AvailableStock = i.AvailableStock,
            IsAvailable = i.IsAvailable
        }).ToList(),
        Tax = cart.Tax,
        ShippingCost = cart.ShippingCost,
        Discount = cart.Discount,
        CouponCode = cart.CouponCode,
        CreatedAt = cart.CreatedAt,
        UpdatedAt = cart.UpdatedAt
    };
}
