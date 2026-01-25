using Microsoft.EntityFrameworkCore;
using UserService.Domain;

namespace UserService.Data;

/// <summary>
/// Database context for user service.
/// </summary>
public class UserDbContext : DbContext
{
    public UserDbContext(DbContextOptions<UserDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<Address> Addresses => Set<Address>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Email).IsRequired().HasMaxLength(256);
            entity.HasIndex(e => e.Email).IsUnique();
            entity.Property(e => e.PasswordHash).IsRequired();
            entity.Property(e => e.FirstName).IsRequired().HasMaxLength(100);
            entity.Property(e => e.LastName).IsRequired().HasMaxLength(100);
            entity.Property(e => e.PhoneNumber).HasMaxLength(20);
            entity.Property(e => e.Role).IsRequired().HasMaxLength(50);
            entity.Property(e => e.RefreshToken).HasMaxLength(500);

            entity.HasMany(e => e.Addresses)
                  .WithOne()
                  .HasForeignKey(a => a.UserId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Address>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Street).IsRequired().HasMaxLength(200);
            entity.Property(e => e.City).IsRequired().HasMaxLength(100);
            entity.Property(e => e.State).IsRequired().HasMaxLength(100);
            entity.Property(e => e.PostalCode).IsRequired().HasMaxLength(20);
            entity.Property(e => e.Country).IsRequired().HasMaxLength(100);
            entity.Property(e => e.AddressType).IsRequired().HasMaxLength(20);
        });

        // Seed demo data
        SeedData(modelBuilder);
    }

    private static void SeedData(ModelBuilder modelBuilder)
    {
        var adminId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var customerId = Guid.Parse("22222222-2222-2222-2222-222222222222");

        // Password: Admin123! and Customer123!
        var adminPasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin123!");
        var customerPasswordHash = BCrypt.Net.BCrypt.HashPassword("Customer123!");

        modelBuilder.Entity<User>().HasData(
            new
            {
                Id = adminId,
                Email = "admin@ecommerce.com",
                PasswordHash = adminPasswordHash,
                FirstName = "System",
                LastName = "Admin",
                Role = "Admin",
                IsActive = true,
                EmailVerified = true,
                CreatedAt = DateTimeOffset.UtcNow
            },
            new
            {
                Id = customerId,
                Email = "customer@example.com",
                PasswordHash = customerPasswordHash,
                FirstName = "John",
                LastName = "Doe",
                PhoneNumber = "+1-555-0100",
                Role = "Customer",
                IsActive = true,
                EmailVerified = true,
                CreatedAt = DateTimeOffset.UtcNow
            }
        );

        modelBuilder.Entity<Address>().HasData(
            new
            {
                Id = Guid.Parse("33333333-3333-3333-3333-333333333333"),
                UserId = customerId,
                Street = "123 Main Street",
                City = "New York",
                State = "NY",
                PostalCode = "10001",
                Country = "USA",
                AddressType = "Shipping",
                IsDefault = true,
                CreatedAt = DateTimeOffset.UtcNow
            }
        );
    }
}
