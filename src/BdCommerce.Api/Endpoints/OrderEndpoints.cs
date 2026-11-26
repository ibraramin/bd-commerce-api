using BdCommerce.Api.Contracts;
using BdCommerce.Api.Data;
using BdCommerce.Api.Domain;
using BdCommerce.Api.Validation;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace BdCommerce.Api.Endpoints;

public static class OrderEndpoints
{
    public static IEndpointRouteBuilder MapOrderEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/v1/orders", async (
                CreateOrderRequest request,
                IValidator<CreateOrderRequest> validator,
                AppDbContext db,
                CancellationToken ct) =>
            {
                var validation = await validator.ValidateAsync(request, ct);
                if (!validation.IsValid)
                {
                    return validation.ToValidationProblem();
                }

                var items = request.Items!;
                var ids = items.Select(i => i.ProductId).Distinct().ToList();
                var products = await db.Products.Where(p => ids.Contains(p.Id))
                    .ToDictionaryAsync(p => p.Id, ct);

                var missingErrors = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase);
                for (var index = 0; index < items.Count; index++)
                {
                    if (!products.ContainsKey(items[index].ProductId))
                    {
                        missingErrors[$"Items[{index}].ProductId"] =
                            new[] { $"Product {items[index].ProductId} does not exist." };
                    }
                }

                if (missingErrors.Count > 0)
                {
                    return Results.BadRequest(new { errors = missingErrors });
                }

                var order = new Order
                {
                    CustomerName = request.CustomerName!,
                    CustomerEmail = request.CustomerEmail!,
                    Status = "Pending",
                    CreatedAt = DateTimeOffset.UtcNow,
                    Items = items.Select(i => new OrderItem
                    {
                        ProductId = i.ProductId,
                        Quantity = i.Quantity,
                        UnitPrice = products[i.ProductId].Price
                    }).ToList()
                };
                order.Total = order.Items.Sum(i => i.Quantity * i.UnitPrice);

                db.Orders.Add(order);
                await db.SaveChangesAsync(ct);

                return Results.Created($"/api/v1/orders/{order.Id}", ToResponse(order));
            })
            .WithName("CreateOrder")
            .WithTags("Orders")
            .Produces<OrderResponse>(StatusCodes.Status201Created)
            .ProducesValidationProblem(StatusCodes.Status400BadRequest);

        app.MapGet("/api/v1/orders/{id:int}", async (
                int id,
                AppDbContext db,
                CancellationToken ct) =>
            {
                var order = await db.Orders.AsNoTracking().FirstOrDefaultAsync(o => o.Id == id, ct);
                return order is null ? Results.NotFound() : Results.Ok(ToResponse(order));
            })
            .WithName("GetOrder")
            .WithTags("Orders")
            .Produces<OrderResponse>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound);

        return app;
    }

    private static OrderResponse ToResponse(Order order) =>
        new(order.Id, order.Status, order.Total, order.CreatedAt.ToString("O"));
}
