using System.Net;
using Xunit;

namespace BdCommerce.Api.Tests;

[Collection("Api")]
public sealed class OrdersTests(ApiFixture fixture)
{
    [Fact]
    public async Task CreateOrder_ReturnsCreated_AndOrderCanBeFetched()
    {
        var created = await TestHelpers.CreateValidOrderAsync(fixture.Client);

        var id = created.GetProperty("id").GetInt32();
        Assert.True(id > 0, "Order id must be a positive integer.");
        Assert.True(created.TryGetProperty("status", out _), "Order response must contain a status.");
        Assert.True(created.TryGetProperty("createdAt", out _), "Order response must contain createdAt.");

        var fetch = await fixture.Client.GetAsync($"/api/v1/orders/{id}");

        Assert.Equal(HttpStatusCode.OK, fetch.StatusCode);

        var fetched = await TestHelpers.ReadJsonAsync(fetch);
        Assert.Equal(id, fetched.GetProperty("id").GetInt32());
    }

    [Fact]
    public async Task CreateOrder_WithInvalidBody_ReturnsBadRequest()
    {
        const string invalidBody = """
            {"customerName":"","customerEmail":"not-an-email","items":[]}
            """;

        var response = await fixture.Client.PostAsync(
            "/api/v1/orders",
            TestHelpers.Json(invalidBody));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
