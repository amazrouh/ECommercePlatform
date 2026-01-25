using Microsoft.EntityFrameworkCore;
using ProductService.Domain;

namespace ProductService.Data;

/// <summary>
/// Database context for product service.
/// </summary>
public class ProductDbContext : DbContext
{
    public ProductDbContext(DbContextOptions<ProductDbContext> options) : base(options) { }

    public DbSet<Product> Products => Set<Product>();
    public DbSet<Category> Categories => Set<Category>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Product>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Description).IsRequired().HasMaxLength(5000);
            entity.Property(e => e.Sku).IsRequired().HasMaxLength(50);
            entity.HasIndex(e => e.Sku).IsUnique();
            entity.Property(e => e.Price).HasPrecision(18, 2);
            entity.Property(e => e.SalePrice).HasPrecision(18, 2);
            entity.Property(e => e.ImageUrls).HasConversion(
                v => string.Join("|||", v),
                v => v.Split("|||", StringSplitOptions.RemoveEmptyEntries).ToList());

            entity.HasOne(e => e.Category)
                  .WithMany(c => c.Products)
                  .HasForeignKey(e => e.CategoryId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Category>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Description).HasMaxLength(500);

            entity.HasOne(e => e.ParentCategory)
                  .WithMany(c => c.SubCategories)
                  .HasForeignKey(e => e.ParentCategoryId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // Seed demo data
        SeedData(modelBuilder);
    }

    private static void SeedData(ModelBuilder modelBuilder)
    {
        var electronicsId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        var clothingId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
        var booksId = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");

        modelBuilder.Entity<Category>().HasData(
            new { Id = electronicsId, Name = "Electronics", Description = "Electronic devices and accessories", IsActive = true, CreatedAt = DateTimeOffset.UtcNow },
            new { Id = clothingId, Name = "Clothing", Description = "Apparel and fashion", IsActive = true, CreatedAt = DateTimeOffset.UtcNow },
            new { Id = booksId, Name = "Books", Description = "Books and publications", IsActive = true, CreatedAt = DateTimeOffset.UtcNow }
        );

        modelBuilder.Entity<Product>().HasData(
            new
            {
                Id = Guid.Parse("11111111-aaaa-aaaa-aaaa-111111111111"),
                Name = "Wireless Bluetooth Headphones",
                Description = "High-quality wireless headphones with noise cancellation and 30-hour battery life.",
                Sku = "ELEC-HP-001",
                Price = 149.99m,
                SalePrice = (decimal?)129.99m,
                StockQuantity = 50,
                CategoryId = electronicsId,
                ImageUrls = "https://example.com/headphones1.jpg|||https://example.com/headphones2.jpg",
                ThumbnailUrl = "https://example.com/headphones1.jpg",
                AverageRating = 4.5,
                ReviewCount = 128,
                IsActive = true,
                CreatedAt = DateTimeOffset.UtcNow
            },
            new
            {
                Id = Guid.Parse("22222222-aaaa-aaaa-aaaa-222222222222"),
                Name = "Smart Watch Pro",
                Description = "Advanced smartwatch with health monitoring, GPS, and water resistance.",
                Sku = "ELEC-SW-001",
                Price = 299.99m,
                StockQuantity = 30,
                CategoryId = electronicsId,
                ImageUrls = "https://example.com/smartwatch1.jpg",
                ThumbnailUrl = "https://example.com/smartwatch1.jpg",
                AverageRating = 4.8,
                ReviewCount = 256,
                IsActive = true,
                CreatedAt = DateTimeOffset.UtcNow
            },
            new
            {
                Id = Guid.Parse("33333333-aaaa-aaaa-aaaa-333333333333"),
                Name = "Classic Cotton T-Shirt",
                Description = "Comfortable 100% cotton t-shirt available in multiple colors.",
                Sku = "CLTH-TS-001",
                Price = 29.99m,
                StockQuantity = 200,
                CategoryId = clothingId,
                ImageUrls = "https://example.com/tshirt1.jpg|||https://example.com/tshirt2.jpg",
                ThumbnailUrl = "https://example.com/tshirt1.jpg",
                AverageRating = 4.2,
                ReviewCount = 89,
                IsActive = true,
                CreatedAt = DateTimeOffset.UtcNow
            },
            new
            {
                Id = Guid.Parse("44444444-aaaa-aaaa-aaaa-444444444444"),
                Name = "Clean Code: A Handbook",
                Description = "A must-read book for software developers on writing clean, maintainable code.",
                Sku = "BOOK-DEV-001",
                Price = 44.99m,
                StockQuantity = 75,
                CategoryId = booksId,
                ImageUrls = "https://example.com/cleancode.jpg",
                ThumbnailUrl = "https://example.com/cleancode.jpg",
                AverageRating = 4.9,
                ReviewCount = 512,
                IsActive = true,
                CreatedAt = DateTimeOffset.UtcNow
            }
        );
    }
}
