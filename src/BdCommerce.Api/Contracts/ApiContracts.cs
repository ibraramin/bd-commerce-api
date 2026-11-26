namespace BdCommerce.Api.Contracts;

// Response shapes are intentionally small and match the published API contract.
public sealed record ProductResponse(int Id, string Name, string Description, decimal Price, string Currency);

public sealed record OrderResponse(int Id, string Status, decimal Total, string CreatedAt);

public sealed record PaymentInitiationResponse(string PaymentIntentId, string Status, string CheckoutUrl);

/// <summary>status is always "ok"; paymentIntentId is echoed for client correlation.</summary>
public sealed record CallbackResponse(string Status, string PaymentIntentId);

public sealed class CreateOrderRequest
{
    public string? CustomerName { get; set; }
    public string? CustomerEmail { get; set; }
    public List<OrderItemRequest>? Items { get; set; }
}

public sealed class OrderItemRequest
{
    public int ProductId { get; set; }
    public int Quantity { get; set; }
}

public sealed class InitiatePaymentRequest
{
    public int OrderId { get; set; }
    public decimal Amount { get; set; }
    public string? IdempotencyKey { get; set; }
}

public sealed class PaymentCallbackRequest
{
    public string? PaymentIntentId { get; set; }
    public string? Status { get; set; }
}
