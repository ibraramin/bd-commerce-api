using System.Net;
using System.Text.Json;
using Xunit;

namespace BdCommerce.Api.Tests;

[Collection("Api")]
public sealed class PaymentsTests(ApiFixture fixture)
{
    [Fact]
    public async Task Initiate_WithSameIdempotencyKey_ReturnsSamePaymentIntentId()
    {
        var order = await TestHelpers.CreateValidOrderAsync(fixture.Client);
        var orderId = order.GetProperty("id").GetInt32();
        var amount = order.GetProperty("total").GetDecimal();
        Assert.True(orderId > 0);

        var idempotencyKey = "idem-" + Guid.NewGuid().ToString("N");

        var first = await TestHelpers.InitiateAsync(
            fixture.Client, orderId, amount, idempotencyKey);
        var second = await TestHelpers.InitiateAsync(
            fixture.Client, orderId, amount, idempotencyKey);

        Assert.Equal(HttpStatusCode.OK, first.Status);
        Assert.Equal(HttpStatusCode.OK, second.Status);

        var firstIntentId = first.Json.GetProperty("paymentIntentId").GetString();
        var secondIntentId = second.Json.GetProperty("paymentIntentId").GetString();

        Assert.False(string.IsNullOrWhiteSpace(firstIntentId));
        Assert.Equal(firstIntentId, secondIntentId);
        Assert.True(first.Json.TryGetProperty("checkoutUrl", out _), "Initiate response must contain checkoutUrl.");
    }

    [Fact]
    public async Task Callback_WithValidSignature_ReturnsOk()
    {
        var (_, body) = await InitiateFreshIntentAsync();

        var response = await SendCallbackAsync(body, TestHelpers.ComputeSignature(fixture.WebhookSecret, body));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Callback_WithInvalidSignature_ReturnsBadRequest()
    {
        var (_, body) = await InitiateFreshIntentAsync();

        var response = await SendCallbackAsync(
            body,
            "0000000000000000000000000000000000000000000000000000000000000000");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Callback_RepeatedWithSamePayload_IsIdempotent()
    {
        var (_, body) = await InitiateFreshIntentAsync();
        var signature = TestHelpers.ComputeSignature(fixture.WebhookSecret, body);

        var first = await SendCallbackAsync(body, signature);
        var second = await SendCallbackAsync(body, signature);

        Assert.Equal(HttpStatusCode.OK, first.StatusCode);
        Assert.Equal(HttpStatusCode.OK, second.StatusCode);

        var firstJson = await TestHelpers.ReadJsonAsync(first);
        var secondJson = await TestHelpers.ReadJsonAsync(second);

        Assert.Equal("ok", firstJson.GetProperty("status").GetString());
        Assert.Equal("ok", secondJson.GetProperty("status").GetString());
    }

    private async Task<(string IntentId, string Body)> InitiateFreshIntentAsync()
    {
        var order = await TestHelpers.CreateValidOrderAsync(fixture.Client);
        var orderId = order.GetProperty("id").GetInt32();
        var amount = order.GetProperty("total").GetDecimal();
        Assert.True(orderId > 0);

        var idempotencyKey = "cb-" + Guid.NewGuid().ToString("N");

        var (status, json) = await TestHelpers.InitiateAsync(
            fixture.Client, orderId, amount, idempotencyKey);

        Assert.Equal(HttpStatusCode.OK, status);

        var intentId = json.GetProperty("paymentIntentId").GetString();
        Assert.False(string.IsNullOrWhiteSpace(intentId));

        var body = JsonSerializer.Serialize(new
        {
            paymentIntentId = intentId,
            status = "succeeded"
        });

        return (intentId!, body);
    }

    private Task<HttpResponseMessage> SendCallbackAsync(string body, string signature)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/payments/callback")
        {
            Content = TestHelpers.Json(body)
        };

        Assert.True(request.Headers.TryAddWithoutValidation("X-Signature", signature));

        return fixture.Client.SendAsync(request);
    }
}
