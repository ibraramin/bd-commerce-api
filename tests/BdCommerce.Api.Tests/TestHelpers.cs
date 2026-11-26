using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Xunit;

namespace BdCommerce.Api.Tests;

internal static class TestHelpers
{
    public static StringContent Json(string body) => new(body, Encoding.UTF8, "application/json");

    public static async Task<JsonElement> ReadJsonAsync(HttpResponseMessage response)
    {
        var body = await response.Content.ReadAsStringAsync();
        using var document = JsonDocument.Parse(body);
        return document.RootElement.Clone();
    }

    public static string ComputeSignature(string secret, string rawBody)
    {
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(rawBody));
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    public static async Task<string> GetFirstProductIdAsync(HttpClient client)
    {
        var response = await client.GetAsync("/api/v1/products");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var root = await ReadJsonAsync(response);
        var products = root.ValueKind == JsonValueKind.Array ? root : FindFirstArray(root);

        Assert.True(
            products.ValueKind == JsonValueKind.Array && products.GetArrayLength() > 0,
            "GET /api/v1/products must expose at least one seeded product so orders can reference it.");

        var first = products[0];

        if (first.ValueKind == JsonValueKind.Object &&
            first.TryGetProperty("id", out var id))
        {
            return id.GetRawText();
        }

        if (first.ValueKind == JsonValueKind.Object &&
            first.TryGetProperty("productId", out var productId))
        {
            return productId.GetRawText();
        }

        return first.GetRawText();
    }

    public static async Task<JsonElement> CreateValidOrderAsync(HttpClient client)
    {
        var productId = await GetFirstProductIdAsync(client);

        var body = $$"""{"customerName":"Test Customer","customerEmail":"test@example.com","items":[{"productId":{{productId}},"quantity":2}]}""";

        var response = await client.PostAsync("/api/v1/orders", Json(body));
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var json = await ReadJsonAsync(response);
        Assert.True(json.TryGetProperty("id", out _), "Order response must contain an id.");
        Assert.True(json.TryGetProperty("total", out _), "Order response must contain a total.");
        return json;
    }

    public static async Task<(HttpStatusCode Status, JsonElement Json)> InitiateAsync(
        HttpClient client,
        int orderId,
        decimal amount,
        string idempotencyKey)
    {
        var body = JsonSerializer.Serialize(new
        {
            orderId,
            amount,
            idempotencyKey
        });

        var response = await client.PostAsync("/api/v1/payments/initiate", Json(body));
        var json = await ReadJsonAsync(response);
        return (response.StatusCode, json);
    }

    private static JsonElement FindFirstArray(JsonElement root)
    {
        if (root.ValueKind != JsonValueKind.Object)
        {
            return default;
        }

        foreach (var property in root.EnumerateObject())
        {
            if (property.Value.ValueKind == JsonValueKind.Array)
            {
                return property.Value;
            }
        }

        return default;
    }
}
