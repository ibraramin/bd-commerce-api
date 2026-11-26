using BdCommerce.Api.Domain;

namespace BdCommerce.Api.Providers;

/// <summary>Result returned by a payment provider when an intent is initiated.</summary>
public sealed record PaymentInitiationResult(string CheckoutUrl, string ProviderReference);

/// <summary>Payment gateway adapter contract. No implementation performs live money movement.</summary>
public interface IPaymentProvider
{
    Task<PaymentInitiationResult> InitiateAsync(PaymentIntent intent, CancellationToken ct);
}
