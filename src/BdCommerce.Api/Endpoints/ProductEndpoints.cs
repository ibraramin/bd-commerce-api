using BdCommerce.Api.Contracts;
using BdCommerce.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace BdCommerce.Api.Endpoints;

public static class ProductEndpoints
{
    public static RouteHandlerBuilder MapProductEndpoints(this IEndpointRouteBuilder app) =>
        app.MapGet("/api/v1/products", async (AppDbContext db, CancellationToken ct) =>
            {
                var products = await db.Products.AsNoTracking()
                    .OrderBy(p => p.Id)
                    .ToListAsync(ct);
                return Results.Ok(products.Select(p => new ProductResponse(
                    p.Id, p.Name, p.Description, p.Price, p.Currency)).ToArray());
            })
            .WithName("GetProducts")
            .WithTags("Products")
            .Produces<ProductResponse[]>(StatusCodes.Status200OK);
}
