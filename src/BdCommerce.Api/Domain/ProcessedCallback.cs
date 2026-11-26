namespace BdCommerce.Api.Domain;

/// <summary>
/// Audit/idempotency marker for a webhook callback that has already been applied.
/// Recording the intent id prevents a repeated callback from changing state twice.
/// </summary>
public sealed class ProcessedCallback
{
    public int Id { get; set; }
    public Guid PaymentIntentId { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTimeOffset ProcessedAt { get; set; }
}
