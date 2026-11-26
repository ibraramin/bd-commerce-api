using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Testcontainers.PostgreSql;
using Xunit;

namespace BdCommerce.Api.Tests;

/// <summary>
/// One PostgreSQL container and one application factory are shared by every test
/// in the "Api" collection. This keeps the heavy container startup out of each
/// individual test while still exercising the real EF Core/Npgsql path.
/// </summary>
public sealed class ApiFixture : IAsyncLifetime
{
    private PostgreSqlContainer? _postgres;
    private string? _previousConnectionString;
    private string? _previousProvider;
    private string? _previousSecret;

    public WebApplicationFactory<Program> Factory { get; private set; } = null!;

    public HttpClient Client { get; private set; } = null!;

    public string WebhookSecret { get; } = "local-test-webhook-secret";

    public async Task InitializeAsync()
    {
        _postgres = new PostgreSqlBuilder()
            .WithImage("postgres:16-alpine")
            .WithDatabase("bdcommerce_tests")
            .WithUsername("postgres")
            .WithPassword("postgres")
            .Build();

        await _postgres.StartAsync();

        var connectionString = _postgres.GetConnectionString();

        // Minimal hosting loads appsettings.json after sources supplied through
        // ConfigureAppConfiguration, so environment variables are the authoritative
        // override here. They map to ConnectionStrings:Default, Payment:Provider and
        // Payment:WebhookSecret in the application configuration.
        _previousConnectionString = Environment.GetEnvironmentVariable("ConnectionStrings__Default");
        _previousProvider = Environment.GetEnvironmentVariable("Payment__Provider");
        _previousSecret = Environment.GetEnvironmentVariable("Payment__WebhookSecret");
        Environment.SetEnvironmentVariable("ConnectionStrings__Default", connectionString);
        Environment.SetEnvironmentVariable("Payment__Provider", "Mock");
        Environment.SetEnvironmentVariable("Payment__WebhookSecret", WebhookSecret);

        Factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.ConfigureAppConfiguration((_, config) =>
                {
                    config.AddInMemoryCollection(new Dictionary<string, string?>
                    {
                        ["ConnectionStrings:Default"] = connectionString,
                        ["Payment:Provider"] = "Mock",
                        ["Payment:WebhookSecret"] = WebhookSecret
                    });
                });
            });

        Client = Factory.CreateClient();
    }

    public async Task DisposeAsync()
    {
        Client?.Dispose();

        if (Factory is not null)
        {
            await Factory.DisposeAsync();
        }

        if (_postgres is not null)
        {
            await _postgres.DisposeAsync();
        }

        Environment.SetEnvironmentVariable("ConnectionStrings__Default", _previousConnectionString);
        Environment.SetEnvironmentVariable("Payment__Provider", _previousProvider);
        Environment.SetEnvironmentVariable("Payment__WebhookSecret", _previousSecret);
    }
}

[CollectionDefinition("Api", DisableParallelization = true)]
public sealed class ApiCollection : ICollectionFixture<ApiFixture>;
