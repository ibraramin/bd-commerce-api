using BdCommerce.Api.Domain;
using Microsoft.EntityFrameworkCore;

namespace BdCommerce.Api.Data;

/// <summary>
/// Creates the schema and seeds the catalog once. A database that is unreachable is
/// tolerated only in Development so local runs (and tooling without Postgres) still boot;
/// failures in Production are always propagated.
/// </summary>
public sealed class DbInitializer
{
    private static readonly Product[] SeedProducts =
    {
        new() { Id = 1, Name = "Laptop Stand", Description = "Aluminium adjustable stand", Price = 1500m, Currency = "BDT" },
        new() { Id = 2, Name = "USB-C Hub", Description = "7-in-1 USB-C adapter", Price = 850m, Currency = "BDT" },
        new() { Id = 3, Name = "Wireless Mouse", Description = "Quiet wireless mouse", Price = 650m, Currency = "BDT" },
        new() { Id = 4, Name = "Mechanical Keyboard", Description = "Compact tenkeyless keyboard", Price = 3200m, Currency = "BDT" },
        new() { Id = 5, Name = "Desk Mat", Description = "Large fabric desk mat", Price = 450m, Currency = "BDT" }
    };

    private readonly AppDbContext _db;
    private readonly IHostEnvironment _environment;
    private readonly ILogger<DbInitializer> _logger;

    public DbInitializer(AppDbContext db, IHostEnvironment environment, ILogger<DbInitializer> logger)
    {
        _db = db;
        _environment = environment;
        _logger = logger;
    }

    public async Task InitializeAsync(CancellationToken ct = default)
    {
        try
        {
            await _db.Database.EnsureCreatedAsync(ct);
            if (!await _db.Products.AnyAsync(ct))
            {
                _db.Products.AddRange(SeedProducts);
                await _db.SaveChangesAsync(ct);
            }
        }
        catch (Exception ex) when (_environment.IsDevelopment())
        {
            _logger.LogWarning(ex, "Database is unavailable; continuing startup in Development.");
        }
    }
}
