namespace ProductService.Domain;

/// <summary>
/// Product entity.
/// </summary>
public class Product
{
    public Guid Id { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public string Sku { get; private set; } = string.Empty;
    public decimal Price { get; private set; }
    public decimal? SalePrice { get; private set; }
    public int StockQuantity { get; private set; }
    public Guid CategoryId { get; private set; }
    public Category? Category { get; private set; }
    public List<string> ImageUrls { get; private set; } = new();
    public string? ThumbnailUrl { get; private set; }
    public double AverageRating { get; private set; }
    public int ReviewCount { get; private set; }
    public bool IsActive { get; private set; } = true;
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset? UpdatedAt { get; private set; }

    private Product() { } // EF Core

    public static Product Create(string name, string description, string sku, decimal price, int stockQuantity, Guid categoryId, List<string>? imageUrls = null)
    {
        return new Product
        {
            Id = Guid.NewGuid(),
            Name = name,
            Description = description,
            Sku = sku.ToUpperInvariant(),
            Price = price,
            StockQuantity = stockQuantity,
            CategoryId = categoryId,
            ImageUrls = imageUrls ?? new List<string>(),
            ThumbnailUrl = imageUrls?.FirstOrDefault(),
            CreatedAt = DateTimeOffset.UtcNow
        };
    }

    public void Update(string? name, string? description, decimal? price, decimal? salePrice, int? stockQuantity, Guid? categoryId, List<string>? imageUrls, bool? isActive)
    {
        if (!string.IsNullOrWhiteSpace(name)) Name = name;
        if (!string.IsNullOrWhiteSpace(description)) Description = description;
        if (price.HasValue) Price = price.Value;
        if (salePrice.HasValue) SalePrice = salePrice.Value;
        if (stockQuantity.HasValue) StockQuantity = stockQuantity.Value;
        if (categoryId.HasValue) CategoryId = categoryId.Value;
        if (imageUrls != null)
        {
            ImageUrls = imageUrls;
            ThumbnailUrl = imageUrls.FirstOrDefault();
        }
        if (isActive.HasValue) IsActive = isActive.Value;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void UpdateStock(int quantity)
    {
        StockQuantity = Math.Max(0, quantity);
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void DeductStock(int quantity)
    {
        StockQuantity = Math.Max(0, StockQuantity - quantity);
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void AddStock(int quantity)
    {
        StockQuantity += quantity;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void UpdateRating(double rating, int reviewCount)
    {
        AverageRating = rating;
        ReviewCount = reviewCount;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void Deactivate()
    {
        IsActive = false;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void Activate()
    {
        IsActive = true;
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}

/// <summary>
/// Product category entity.
/// </summary>
public class Category
{
    public Guid Id { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public string? ImageUrl { get; private set; }
    public Guid? ParentCategoryId { get; private set; }
    public Category? ParentCategory { get; private set; }
    public bool IsActive { get; private set; } = true;
    public DateTimeOffset CreatedAt { get; private set; }

    private readonly List<Product> _products = new();
    public IReadOnlyCollection<Product> Products => _products.AsReadOnly();

    private readonly List<Category> _subCategories = new();
    public IReadOnlyCollection<Category> SubCategories => _subCategories.AsReadOnly();

    private Category() { } // EF Core

    public static Category Create(string name, string? description = null, string? imageUrl = null, Guid? parentCategoryId = null)
    {
        return new Category
        {
            Id = Guid.NewGuid(),
            Name = name,
            Description = description,
            ImageUrl = imageUrl,
            ParentCategoryId = parentCategoryId,
            CreatedAt = DateTimeOffset.UtcNow
        };
    }

    public void Update(string? name, string? description, string? imageUrl)
    {
        if (!string.IsNullOrWhiteSpace(name)) Name = name;
        if (description != null) Description = description;
        if (imageUrl != null) ImageUrl = imageUrl;
    }
}
