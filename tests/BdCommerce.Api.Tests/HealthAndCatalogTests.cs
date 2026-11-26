using System.Net;
using Xunit;

namespace BdCommerce.Api.Tests;

[Collection("Api")]
public sealed class HealthAndCatalogTests(ApiFixture fixture)
{
    [Fact]
    public async Task Health_ReturnsOkWithOkBody()
    {
        var response = await fixture.Client.GetAsync("/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = (await response.Content.ReadAsStringAsync()).Trim();
        Assert.Equal("ok", body.Trim('"'));
    }

    [Fact]
    public async Task Products_ReturnsSeededProducts()
    {
        var productId = await TestHelpers.GetFirstProductIdAsync(fixture.Client);

        Assert.False(string.IsNullOrWhiteSpace(productId));
    }

    [Fact]
    public async Task SwaggerDocument_IsServed()
    {
        var response = await fixture.Client.GetAsync("/swagger/v1/swagger.json");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("openapi", body, StringComparison.OrdinalIgnoreCase);
    }
}
