using BdCommerce.Api.Domain;
using Microsoft.EntityFrameworkCore;

namespace BdCommerce.Api.Data;

public sealed class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<Product> Products => Set<Product>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();
    public DbSet<PaymentIntent> PaymentIntents => Set<PaymentIntent>();
    public DbSet<ProcessedCallback> ProcessedCallbacks => Set<ProcessedCallback>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Product>(entity =>
        {
            entity.HasKey(p => p.Id);
            entity.Property(p => p.Name).IsRequired().HasMaxLength(200);
            entity.Property(p => p.Description).IsRequired().HasMaxLength(1000);
            entity.Property(p => p.Currency).IsRequired().HasMaxLength(8);
            entity.Property(p => p.Price).HasPrecision(18, 2);
        });

        modelBuilder.Entity<Order>(entity =>
        {
            entity.HasKey(o => o.Id);
            entity.Property(o => o.CustomerName).IsRequired().HasMaxLength(200);
            entity.Property(o => o.CustomerEmail).IsRequired().HasMaxLength(320);
            entity.Property(o => o.Status).IsRequired().HasMaxLength(50);
            entity.Property(o => o.Total).HasPrecision(18, 2);
        });

        modelBuilder.Entity<OrderItem>(entity =>
        {
            entity.HasKey(i => i.Id);
            entity.HasOne(i => i.Order).WithMany(o => o.Items)
                .HasForeignKey(i => i.OrderId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(i => i.Product).WithMany()
                .HasForeignKey(i => i.ProductId).OnDelete(DeleteBehavior.Restrict);
            entity.Property(i => i.UnitPrice).HasPrecision(18, 2);
        });

        modelBuilder.Entity<PaymentIntent>(entity =>
        {
            entity.HasKey(p => p.Id);
            entity.HasIndex(p => p.PublicId).IsUnique();
            entity.HasIndex(p => p.IdempotencyKey).IsUnique()
                .HasFilter("\"IdempotencyKey\" IS NOT NULL");
            entity.Property(p => p.Amount).HasPrecision(18, 2);
            entity.Property(p => p.IdempotencyKey).HasMaxLength(200);
            entity.Property(p => p.Status).IsRequired().HasMaxLength(50);
            entity.Property(p => p.Provider).IsRequired().HasMaxLength(50);
        });

        modelBuilder.Entity<ProcessedCallback>(entity =>
        {
            entity.HasKey(c => c.Id);
            entity.HasIndex(c => c.PaymentIntentId).IsUnique();
            entity.Property(c => c.Status).IsRequired().HasMaxLength(50);
        });
    }
}
