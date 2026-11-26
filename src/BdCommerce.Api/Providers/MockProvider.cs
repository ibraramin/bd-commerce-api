using BdCommerce.Api.Domain;

namespace BdCommerce.Api.Providers;

/// <summary>
/// Default provider. Fully deterministic, offline, and safe: no network calls and no real
/// money. The checkout URL embeds the public intent id so it is stable for a given intent.
/// </summary>
public sealed class MockProvider : IPaymentProvider
{
    public Task<PaymentInitiationResult> InitiateAsync(PaymentIntent intent, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        var id = intent.PublicId;
        return Task.FromResult(new PaymentInitiationResult(
            $"https://sandbox.local/checkout/{id}",
            $"mock-{id}"));
    }
}
