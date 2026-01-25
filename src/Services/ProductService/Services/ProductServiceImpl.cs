using Microsoft.EntityFrameworkCore;
using ProductService.Data;
using ProductService.Domain;
using Shared.Contracts.DTOs;

namespace ProductService.Services;

/// <summary>
/// Product service implementation.
/// </summary>
public class ProductServiceImpl : IProductService
{
    private readonly ProductDbContext _context;
    private readonly ILogger<ProductServiceImpl> _logger;

    public ProductServiceImpl(ProductDbContext context, ILogger<ProductServiceImpl> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<PagedResult<ProductSummaryDto>> SearchAsync(ProductSearchRequest request, CancellationToken cancellationToken = default)
    {
        var query = _context.Products
            .Include(p => p.Category)
            .Where(p => p.IsActive)
            .AsQueryable();

        // Apply filters
        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            var term = request.SearchTerm.ToLower();
            query = query.Where(p => p.Name.ToLower().Contains(term) || p.Description.ToLower().Contains(term) || p.Sku.ToLower().Contains(term));
        }

        if (request.CategoryId.HasValue)
        {
            query = query.Where(p => p.CategoryId == request.CategoryId.Value);
        }

        if (request.MinPrice.HasValue)
        {
            query = query.Where(p => (p.SalePrice ?? p.Price) >= request.MinPrice.Value);
        }

        if (request.MaxPrice.HasValue)
        {
            query = query.Where(p => (p.SalePrice ?? p.Price) <= request.MaxPrice.Value);
        }

        if (request.InStockOnly == true)
        {
            query = query.Where(p => p.StockQuantity > 0);
        }

        // Apply sorting
        query = request.SortBy?.ToLower() switch
        {
            "price" => request.SortDescending ? query.OrderByDescending(p => p.SalePrice ?? p.Price) : query.OrderBy(p => p.SalePrice ?? p.Price),
            "rating" => request.SortDescending ? query.OrderByDescending(p => p.AverageRating) : query.OrderBy(p => p.AverageRating),
            "newest" => query.OrderByDescending(p => p.CreatedAt),
            _ => request.SortDescending ? query.OrderByDescending(p => p.Name) : query.OrderBy(p => p.Name)
        };

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(p => new ProductSummaryDto
            {
                Id = p.Id,
                Name = p.Name,
                Price = p.Price,
                SalePrice = p.SalePrice,
                ThumbnailUrl = p.ThumbnailUrl,
                AverageRating = p.AverageRating,
                ReviewCount = p.ReviewCount,
                IsInStock = p.StockQuantity > 0
            })
            .ToListAsync(cancellationToken);

        return new PagedResult<ProductSummaryDto>
        {
            Items = items,
            TotalCount = totalCount,
            Page = request.Page,
            PageSize = request.PageSize
        };
    }

    public async Task<ProductDto?> GetByIdAsync(Guid productId, CancellationToken cancellationToken = default)
    {
        var product = await _context.Products
            .Include(p => p.Category)
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == productId, cancellationToken);

        return product == null ? null : MapToDto(product);
    }

    public async Task<ProductDto?> GetBySkuAsync(string sku, CancellationToken cancellationToken = default)
    {
        var product = await _context.Products
            .Include(p => p.Category)
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Sku == sku.ToUpperInvariant(), cancellationToken);

        return product == null ? null : MapToDto(product);
    }

    public async Task<ProductDto> CreateAsync(CreateProductRequest request, CancellationToken cancellationToken = default)
    {
        var product = Product.Create(
            request.Name, request.Description, request.Sku,
            request.Price, request.StockQuantity, request.CategoryId,
            request.ImageUrls);

        if (request.SalePrice.HasValue)
        {
            product.Update(null, null, null, request.SalePrice, null, null, null, null);
        }

        _context.Products.Add(product);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Product created: {ProductId} - {Name}", product.Id, product.Name);

        return MapToDto(product);
    }

    public async Task<ProductDto?> UpdateAsync(Guid productId, UpdateProductRequest request, CancellationToken cancellationToken = default)
    {
        var product = await _context.Products
            .Include(p => p.Category)
            .FirstOrDefaultAsync(p => p.Id == productId, cancellationToken);

        if (product == null) return null;

        product.Update(request.Name, request.Description, request.Price, request.SalePrice,
            request.StockQuantity, request.CategoryId, request.ImageUrls, request.IsActive);

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Product updated: {ProductId}", productId);
        return MapToDto(product);
    }

    public async Task<bool> DeleteAsync(Guid productId, CancellationToken cancellationToken = default)
    {
        var product = await _context.Products.FindAsync(new object[] { productId }, cancellationToken);
        if (product == null) return false;

        product.Deactivate();
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Product deactivated: {ProductId}", productId);
        return true;
    }

    public async Task<bool> UpdateStockAsync(Guid productId, int quantity, CancellationToken cancellationToken = default)
    {
        var product = await _context.Products.FindAsync(new object[] { productId }, cancellationToken);
        if (product == null) return false;

        product.UpdateStock(quantity);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Stock updated for product {ProductId}: {Quantity}", productId, quantity);
        return true;
    }

    public async Task<IEnumerable<CategoryDto>> GetCategoriesAsync(CancellationToken cancellationToken = default)
    {
        var categories = await _context.Categories
            .Include(c => c.Products)
            .Where(c => c.IsActive)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        return categories.Select(c => new CategoryDto
        {
            Id = c.Id,
            Name = c.Name,
            Description = c.Description,
            ImageUrl = c.ImageUrl,
            ParentCategoryId = c.ParentCategoryId,
            ProductCount = c.Products.Count(p => p.IsActive),
            IsActive = c.IsActive
        });
    }

    public async Task<CategoryDto?> GetCategoryByIdAsync(Guid categoryId, CancellationToken cancellationToken = default)
    {
        var category = await _context.Categories
            .Include(c => c.Products)
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == categoryId, cancellationToken);

        if (category == null) return null;

        return new CategoryDto
        {
            Id = category.Id,
            Name = category.Name,
            Description = category.Description,
            ImageUrl = category.ImageUrl,
            ParentCategoryId = category.ParentCategoryId,
            ProductCount = category.Products.Count(p => p.IsActive),
            IsActive = category.IsActive
        };
    }

    public async Task<CategoryDto> CreateCategoryAsync(CreateCategoryRequest request, CancellationToken cancellationToken = default)
    {
        var category = Category.Create(request.Name, request.Description, request.ImageUrl, request.ParentCategoryId);

        _context.Categories.Add(category);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Category created: {CategoryId} - {Name}", category.Id, category.Name);

        return new CategoryDto
        {
            Id = category.Id,
            Name = category.Name,
            Description = category.Description,
            ImageUrl = category.ImageUrl,
            ParentCategoryId = category.ParentCategoryId,
            ProductCount = 0,
            IsActive = category.IsActive
        };
    }

    private static ProductDto MapToDto(Product product) => new()
    {
        Id = product.Id,
        Name = product.Name,
        Description = product.Description,
        Sku = product.Sku,
        Price = product.Price,
        SalePrice = product.SalePrice,
        StockQuantity = product.StockQuantity,
        CategoryId = product.CategoryId,
        CategoryName = product.Category?.Name ?? string.Empty,
        ImageUrls = product.ImageUrls,
        ThumbnailUrl = product.ThumbnailUrl,
        AverageRating = product.AverageRating,
        ReviewCount = product.ReviewCount,
        IsActive = product.IsActive,
        CreatedAt = product.CreatedAt
    };
}
