using System.ComponentModel.DataAnnotations;

namespace Shared.Contracts.DTOs;

/// <summary>
/// Product data transfer object.
/// </summary>
public record ProductDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string Sku { get; init; } = string.Empty;
    public decimal Price { get; init; }
    public decimal? SalePrice { get; init; }
    public int StockQuantity { get; init; }
    public bool IsInStock => StockQuantity > 0;
    public Guid CategoryId { get; init; }
    public string CategoryName { get; init; } = string.Empty;
    public List<string> ImageUrls { get; init; } = new();
    public string? ThumbnailUrl { get; init; }
    public double AverageRating { get; init; }
    public int ReviewCount { get; init; }
    public bool IsActive { get; init; } = true;
    public DateTimeOffset CreatedAt { get; init; }
}

/// <summary>
/// Product summary for listings.
/// </summary>
public record ProductSummaryDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public decimal Price { get; init; }
    public decimal? SalePrice { get; init; }
    public string? ThumbnailUrl { get; init; }
    public double AverageRating { get; init; }
    public int ReviewCount { get; init; }
    public bool IsInStock { get; init; }
}

/// <summary>
/// Create product request.
/// </summary>
public record CreateProductRequest
{
    [Required]
    [StringLength(200)]
    public string Name { get; init; } = string.Empty;

    [Required]
    [StringLength(5000)]
    public string Description { get; init; } = string.Empty;

    [Required]
    [StringLength(50)]
    public string Sku { get; init; } = string.Empty;

    [Required]
    [Range(0.01, 999999.99)]
    public decimal Price { get; init; }

    [Range(0.01, 999999.99)]
    public decimal? SalePrice { get; init; }

    [Required]
    [Range(0, int.MaxValue)]
    public int StockQuantity { get; init; }

    [Required]
    public Guid CategoryId { get; init; }

    public List<string>? ImageUrls { get; init; }
}

/// <summary>
/// Update product request.
/// </summary>
public record UpdateProductRequest
{
    [StringLength(200)]
    public string? Name { get; init; }

    [StringLength(5000)]
    public string? Description { get; init; }

    [Range(0.01, 999999.99)]
    public decimal? Price { get; init; }

    [Range(0.01, 999999.99)]
    public decimal? SalePrice { get; init; }

    [Range(0, int.MaxValue)]
    public int? StockQuantity { get; init; }

    public Guid? CategoryId { get; init; }

    public List<string>? ImageUrls { get; init; }

    public bool? IsActive { get; init; }
}

/// <summary>
/// Category data transfer object.
/// </summary>
public record CategoryDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public string? ImageUrl { get; init; }
    public Guid? ParentCategoryId { get; init; }
    public int ProductCount { get; init; }
    public bool IsActive { get; init; } = true;
}

/// <summary>
/// Create category request.
/// </summary>
public record CreateCategoryRequest
{
    [Required]
    [StringLength(100)]
    public string Name { get; init; } = string.Empty;

    [StringLength(500)]
    public string? Description { get; init; }

    public string? ImageUrl { get; init; }

    public Guid? ParentCategoryId { get; init; }
}

/// <summary>
/// Product search/filter request.
/// </summary>
public record ProductSearchRequest
{
    public string? SearchTerm { get; init; }
    public Guid? CategoryId { get; init; }
    public decimal? MinPrice { get; init; }
    public decimal? MaxPrice { get; init; }
    public bool? InStockOnly { get; init; }
    public string? SortBy { get; init; } = "Name"; // Name, Price, Rating, Newest
    public bool SortDescending { get; init; }
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 20;
}

/// <summary>
/// Paginated result wrapper.
/// </summary>
public record PagedResult<T>
{
    public List<T> Items { get; init; } = new();
    public int TotalCount { get; init; }
    public int Page { get; init; }
    public int PageSize { get; init; }
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
    public bool HasNextPage => Page < TotalPages;
    public bool HasPreviousPage => Page > 1;
}
