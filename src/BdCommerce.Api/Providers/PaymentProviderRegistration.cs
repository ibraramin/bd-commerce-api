namespace BdCommerce.Api.Providers;

public static class PaymentProviderRegistration
{
    /// <summary>
    /// Registers the configured provider. Anything other than "BkashSandbox" falls back to
    /// the offline Mock provider, so a misconfiguration can never select a live gateway.
    /// </summary>
    public static IServiceCollection AddPaymentProviders(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var configured = configuration["Payment:Provider"];
        IPaymentProvider provider = string.Equals(configured, "BkashSandbox", StringComparison.OrdinalIgnoreCase)
            ? new BkashSandboxProvider()
            : new MockProvider();

        services.AddSingleton(provider);
        services.AddSingleton(provider.GetType(), provider);
        return services;
    }
}
