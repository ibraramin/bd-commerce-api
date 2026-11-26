using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using BdCommerce.Api.Contracts;
using BdCommerce.Api.Data;
using BdCommerce.Api.Domain;
using BdCommerce.Api.Providers;
using BdCommerce.Api.Validation;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace BdCommerce.Api.Endpoints;

public static class PaymentEndpoints
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public static IEndpointRouteBuilder MapPaymentEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/v1/payments/initiate", async (
                InitiatePaymentRequest request,
                IValidator<InitiatePaymentRequest> validator,
                AppDbContext db,
                IPaymentProvider provider,
                IConfiguration configuration,
                CancellationToken ct) =>
            {
                var validation = await validator.ValidateAsync(request, ct);
                if (!validation.IsValid)
                {
                    return validation.ToValidationProblem();
                }

                if (!await db.Orders.AnyAsync(o => o.Id == request.OrderId, ct))
                {
                    return Results.BadRequest(new
                    {
                        errors = new Dictionary<string, string[]>
                        {
                            ["OrderId"] = new[] { $"Order {request.OrderId} does not exist." }
                        }
                    });
                }

                // Idempotency: a previously stored intent for this key is returned unchanged.
                var key = string.IsNullOrWhiteSpace(request.IdempotencyKey) ? null : request.IdempotencyKey.Trim();
                if (key is not null)
                {
                    var existing = await db.PaymentIntents.AsNoTracking()
                        .FirstOrDefaultAsync(p => p.IdempotencyKey == key, ct);
                    if (existing is not null)
                    {
                        return Results.Ok(ToResponse(existing));
                    }
                }

                var intent = new PaymentIntent
                {
                    OrderId = request.OrderId,
                    Amount = request.Amount,
                    IdempotencyKey = key,
                    Status = "Pending",
                    Provider = configuration["Payment:Provider"] ?? "Mock",
                    CreatedAt = DateTimeOffset.UtcNow
                };

                var initiation = await provider.InitiateAsync(intent, ct);
                intent.CheckoutUrl = initiation.CheckoutUrl;
                intent.ProviderReference = initiation.ProviderReference;

                db.PaymentIntents.Add(intent);
                await db.SaveChangesAsync(ct);

                return Results.Ok(ToResponse(intent));
            })
            .WithName("InitiatePayment")
            .WithTags("Payments")
            .Produces<PaymentInitiationResponse>(StatusCodes.Status200OK)
            .ProducesValidationProblem(StatusCodes.Status400BadRequest);

        app.MapPost("/api/v1/payments/callback", async (
                HttpRequest httpRequest,
                IValidator<PaymentCallbackRequest> validator,
                AppDbContext db,
                IConfiguration configuration,
                CancellationToken ct) =>
            {
                // Signatures are computed over the EXACT raw bytes received, never over a
                // reserialized DTO (which could reorder properties or change formatting).
                var rawBody = await ReadRawBodyAsync(httpRequest, ct);
                var secret = configuration["Payment:WebhookSecret"] ?? string.Empty;
                var suppliedSignature = httpRequest.Headers["X-Signature"].ToString();

                if (!IsValidSignature(secret, rawBody, suppliedSignature))
                {
                    return Results.BadRequest(new { error = "Missing or invalid X-Signature." });
                }

                var callback = JsonSerializer.Deserialize<PaymentCallbackRequest>(rawBody, JsonOptions);
                if (callback is null)
                {
                    return Results.BadRequest(new { error = "Request body must be a JSON object." });
                }

                var validation = await validator.ValidateAsync(callback, ct);
                if (!validation.IsValid)
                {
                    return validation.ToValidationProblem();
                }

                if (!Guid.TryParse(callback.PaymentIntentId, out var publicId))
                {
                    return Results.BadRequest(new
                    {
                        errors = new Dictionary<string, string[]>
                        {
                            ["PaymentIntentId"] = new[] { "PaymentIntentId must be a GUID." }
                        }
                    });
                }

                var intent = await db.PaymentIntents.AsNoTracking()
                    .FirstOrDefaultAsync(p => p.PublicId == publicId, ct);
                if (intent is null)
                {
                    return Results.NotFound();
                }

                // Idempotency: record processed intent ids and never apply a callback twice.
                if (await db.ProcessedCallbacks.AsNoTracking()
                        .AnyAsync(c => c.PaymentIntentId == publicId, ct))
                {
                    return Results.Ok(new CallbackResponse("ok", publicId.ToString()));
                }

                db.ProcessedCallbacks.Add(new ProcessedCallback
                {
                    PaymentIntentId = publicId,
                    Status = callback.Status!,
                    ProcessedAt = DateTimeOffset.UtcNow
                });
                db.PaymentIntents.Attach(intent);
                intent.Status = callback.Status!;
                await db.SaveChangesAsync(ct);

                return Results.Ok(new CallbackResponse("ok", publicId.ToString()));
            })
            .WithName("PaymentCallback")
            .WithTags("Payments")
            .Produces<CallbackResponse>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status404NotFound);

        return app;
    }

    private static async Task<byte[]> ReadRawBodyAsync(HttpRequest request, CancellationToken ct)
    {
        using var buffer = new MemoryStream();
        await request.Body.CopyToAsync(buffer, ct);
        return buffer.ToArray();
    }

    private static bool IsValidSignature(string secret, byte[] rawBody, string? suppliedSignature)
    {
        if (string.IsNullOrWhiteSpace(suppliedSignature))
        {
            return false;
        }

        byte[] supplied;
        try
        {
            supplied = Convert.FromHexString(suppliedSignature.Trim());
        }
        catch (FormatException)
        {
            return false;
        }

        if (supplied.Length != 32)
        {
            return false;
        }

        var expected = HMACSHA256.HashData(Encoding.UTF8.GetBytes(secret), rawBody);
        return CryptographicOperations.FixedTimeEquals(supplied, expected);
    }

    private static PaymentInitiationResponse ToResponse(PaymentIntent intent) =>
        new(intent.PublicId.ToString(), intent.Status, intent.CheckoutUrl ?? string.Empty);
}
