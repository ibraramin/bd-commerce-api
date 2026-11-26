namespace BdCommerce.Api.Domain;

/// <summary>
/// One payment initiation. <see cref="PublicId"/> is the value exposed to clients as
/// <c>paymentIntentId</c>; <see cref="Id"/> is the database primary key.
/// </summary>
public sealed class PaymentIntent
{
    public int Id { get; set; }
    public Guid PublicId { get; set; } = Guid.NewGuid();
    public int OrderId { get; set; }
    public string? IdempotencyKey { get; set; }
    public decimal Amount { get; set; }
    public string Status { get; set; } = "Pending";
    public string Provider { get; set; } = "Mock";
    public string? ProviderReference { get; set; }
    public string? CheckoutUrl { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}
