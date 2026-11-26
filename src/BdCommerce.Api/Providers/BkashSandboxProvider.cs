using BdCommerce.Api.Domain;

namespace BdCommerce.Api.Providers;

/// <summary>
/// ADAPTER ONLY - documentation scaffold for the bKash sandbox Checkout/Execute contract.
///
/// Sandbox contract (for a future real adapter, deliberately NOT implemented here):
///   1. POST {base-url}/tokenized/checkout/token/grant with app_key/app_secret and a
///      X-APP-Key header; the response yields an id_token.
///   2. POST {base-url}/tokenized/checkout/create with amount, currency "BDT", the
///      merchant callback URL and a client-reference derived from the intent id.
///   3. Redirect the buyer to the returned bkashURL; bKash later calls the merchant
///      callback with the paymentID/status.
/// Sandbox URLs live under sandbox.bkash.example (example host, not the live API).
///
/// This adapter performs NO network calls and moves NO money. Credentials are read only
/// from environment variables and are never hardcoded, logged, or returned to clients.
/// It is never selected by default (Payment:Provider defaults to "Mock").
/// </summary>
public sealed class BkashSandboxProvider : IPaymentProvider
{
    private const string AppKeyVariable = "BKASH_SANDBOX_APP_KEY";
    private const string AppSecretVariable = "BKASH_SANDBOX_APP_SECRET";
    private const string BaseUrlVariable = "BKASH_SANDBOX_BASE_URL";

    public Task<PaymentInitiationResult> InitiateAsync(PaymentIntent intent, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        // Read (but never echo) sandbox credential material. Missing values are reported
        // by variable NAME only, so secrets cannot leak through logs or exceptions.
        var appKey = Environment.GetEnvironmentVariable(AppKeyVariable);
        var appSecret = Environment.GetEnvironmentVariable(AppSecretVariable);
        var baseUrl = Environment.GetEnvironmentVariable(BaseUrlVariable);

        if (string.IsNullOrWhiteSpace(appKey) || string.IsNullOrWhiteSpace(appSecret))
        {
            throw new InvalidOperationException(
                $"{AppKeyVariable} and {AppSecretVariable} must be set to use the bKash sandbox adapter.");
        }

        var sandboxBase = string.IsNullOrWhiteSpace(baseUrl)
            ? "https://sandbox.bkash.example/tokenized/checkout"
            : baseUrl!.TrimEnd('/');

        // Deterministic adapter response; the real token/grant + create calls are intentionally
        // omitted. Local variables are consumed here so no credential is retained or emitted.
        _ = (appKey, appSecret);
        var id = intent.PublicId;
        return Task.FromResult(new PaymentInitiationResult(
            $"{sandboxBase}/{id}",
            $"bkash-sandbox-{id}"));
    }
}
