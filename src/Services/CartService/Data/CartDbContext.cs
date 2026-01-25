using CartService.Domain;
using Microsoft.EntityFrameworkCore;

namespace CartService.Data;

/// <summary>
/// Database context for cart service.
/// </summary>
public class CartDbContext : DbContext
{
    public CartDbContext(DbContextOptions<CartDbContext> options) : base(options) { }

    public DbSet<Cart> Carts => Set<Cart>();
    public DbSet<CartItem> CartItems => Set<CartItem>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Cart>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.UserId).IsUnique();
            entity.Property(e => e.CouponCode).HasMaxLength(50);
            entity.Property(e => e.Discount).HasPrecision(18, 2);

            entity.HasMany(e => e.Items)
                  .WithOne()
                  .HasForeignKey(i => i.CartId)
                  .OnDelete(DeleteBehavior.Cascade);

            // Ignore computed properties
            entity.Ignore(e => e.Subtotal);
            entity.Ignore(e => e.Tax);
            entity.Ignore(e => e.ShippingCost);
            entity.Ignore(e => e.Total);
            entity.Ignore(e => e.ItemCount);
        });

        modelBuilder.Entity<CartItem>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.ProductName).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Sku).IsRequired().HasMaxLength(50);
            entity.Property(e => e.UnitPrice).HasPrecision(18, 2);
            entity.Property(e => e.SalePrice).HasPrecision(18, 2);

            // Ignore computed properties
            entity.Ignore(e => e.EffectivePrice);
            entity.Ignore(e => e.LineTotal);
        });
    }
}
