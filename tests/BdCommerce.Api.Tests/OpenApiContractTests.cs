using System.Net;
using System.Text;
using System.Text.Json;
using Xunit;

namespace BdCommerce.Api.Tests;

/// <summary>
/// Contract tests assert the published OpenAPI document describes the intended API
/// surface: version metadata, the exact path set, operations/tags, request-body
/// schemas, declared response codes and response schemas. They deliberately inspect
/// the contract document rather than repeating the behavioural assertions in
/// HealthAndCatalogTests, OrdersTests and PaymentsTests.
/// </summary>
[Collection("Api")]
public sealed class OpenApiContractTests(ApiFixture fixture)
{
    [Fact]
    public async Task Document_DeclaresOpenApi3WithExpectedInfo()
    {
        var root = await LoadDocumentAsync();

        Assert.Equal("3.0.1", root.GetProperty("openapi").GetString());

        var info = root.GetProperty("info");
        Assert.Equal("BdCommerce.Api", info.GetProperty("title").GetString());
        Assert.Equal("v1", info.GetProperty("version").GetString());
    }

    [Fact]
    public async Task Document_ExposesExactlyTheExpectedPathSet()
    {
        var root = await LoadDocumentAsync();

        var actual = root.GetProperty("paths").EnumerateObject()
            .Select(property => property.Name)
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToArray();

        Assert.Equal(
            new[]
            {
                "/api/v1/orders",
                "/api/v1/orders/{id}",
                "/api/v1/payments/callback",
                "/api/v1/payments/initiate",
                "/api/v1/products",
                "/health"
            },
            actual);
    }

    [Theory]
    [InlineData("/health", "get", "Health", "Health")]
    [InlineData("/api/v1/products", "get", "GetProducts", "Products")]
    [InlineData("/api/v1/orders", "post", "CreateOrder", "Orders")]
    [InlineData("/api/v1/orders/{id}", "get", "GetOrder", "Orders")]
    [InlineData("/api/v1/payments/initiate", "post", "InitiatePayment", "Payments")]
    [InlineData("/api/v1/payments/callback", "post", "PaymentCallback", "Payments")]
    public async Task Document_PublishesExpectedOperationIdAndTag(
        string path, string method, string operationId, string tag)
    {
        var root = await LoadDocumentAsync();

        var operation = root.GetProperty("paths").GetProperty(path).GetProperty(method);

        Assert.Equal(operationId, operation.GetProperty("operationId").GetString());
        Assert.Contains(tag, operation.GetProperty("tags").EnumerateArray()
            .Select(item => item.GetString()));
    }

    [Theory]
    [InlineData("/health", "get", "200")]
    [InlineData("/api/v1/products", "get", "200")]
    [InlineData("/api/v1/orders", "post", "201", "400")]
    [InlineData("/api/v1/orders/{id}", "get", "200", "404")]
    [InlineData("/api/v1/payments/initiate", "post", "200", "400")]
    [InlineData("/api/v1/payments/callback", "post", "200", "400", "404")]
    public async Task Document_DeclaresExactlyTheExpectedResponseCodes(
        string path, string method, params string[] expected)
    {
        var root = await LoadDocumentAsync();

        var responses = root.GetProperty("paths").GetProperty(path)
            .GetProperty(method).GetProperty("responses");

        foreach (var code in expected)
        {
            Assert.True(responses.TryGetProperty(code, out _),
                $"{method} {path} must document a {code} response.");
        }

        Assert.Equal(expected.Length, responses.EnumerateObject().Count());
    }

    [Theory]
    [InlineData("/api/v1/orders", "CreateOrderRequest", "customerName", "customerEmail", "items")]
    [InlineData("/api/v1/payments/initiate", "InitiatePaymentRequest", "orderId", "amount", "idempotencyKey")]
    public async Task Document_PostEndpointsDeclareJsonRequestBodyWithExpectedSchema(
        string path, string schemaName, params string[] expectedProperties)
    {
        var root = await LoadDocumentAsync();

        var jsonBody = root.GetProperty("paths").GetProperty(path)
            .GetProperty("post").GetProperty("requestBody")
            .GetProperty("content").GetProperty("application/json");

        Assert.Equal($"#/components/schemas/{schemaName}",
            jsonBody.GetProperty("schema").GetProperty("$ref").GetString());

        AssertSchemaProperties(root, schemaName, expectedProperties);
    }

    [Theory]
    [InlineData("/api/v1/orders", "post", "201", "OrderResponse", "id", "status", "total", "createdAt")]
    [InlineData("/api/v1/orders/{id}", "get", "200", "OrderResponse", "id", "status", "total", "createdAt")]
    [InlineData("/api/v1/payments/initiate", "post", "200", "PaymentInitiationResponse", "paymentIntentId", "status", "checkoutUrl")]
    [InlineData("/api/v1/payments/callback", "post", "200", "CallbackResponse", "status", "paymentIntentId")]
    public async Task Document_SuccessResponsesReferenceSchemasWithExpectedProperties(
        string path, string method, string code, string schemaName, params string[] expectedProperties)
    {
        var root = await LoadDocumentAsync();

        var response = root.GetProperty("paths").GetProperty(path)
            .GetProperty(method).GetProperty("responses").GetProperty(code);

        var schema = response.GetProperty("content")
            .GetProperty("application/json").GetProperty("schema");

        Assert.Equal($"#/components/schemas/{schemaName}", schema.GetProperty("$ref").GetString());

        AssertSchemaProperties(root, schemaName, expectedProperties);
    }

    [Fact]
    public async Task Document_ProductsSuccessResponseIsArrayOfProductResponse()
    {
        var root = await LoadDocumentAsync();

        var schema = root.GetProperty("paths").GetProperty("/api/v1/products")
            .GetProperty("get").GetProperty("responses").GetProperty("200")
            .GetProperty("content").GetProperty("application/json").GetProperty("schema");

        Assert.Equal("array", schema.GetProperty("type").GetString());
        Assert.Equal("#/components/schemas/ProductResponse",
            schema.GetProperty("items").GetProperty("$ref").GetString());

        AssertSchemaProperties(root, "ProductResponse", "id", "name", "description", "price", "currency");
    }

    [Fact]
    public async Task Document_DeclaresNoSecuritySchemesOrRequirements()
    {
        var root = await LoadDocumentAsync();

        // All endpoints are anonymous by design; auth/RBAC is an explicit non-goal.
        Assert.False(root.TryGetProperty("security", out _));
        Assert.False(root.GetProperty("components").TryGetProperty("securitySchemes", out _));
    }

    // Folded from the external writer's BdCommerce.Api.ContractTests project:
    // golden-snapshot drift guard, problem-details 400 contract, and the string health response.

    [Fact]
    public async Task GeneratedDocument_MatchesCommittedGoldenSnapshot()
    {
        var root = await LoadDocumentAsync();

        var actual = Canonical(root);
        var goldenPath = Path.Combine(AppContext.BaseDirectory, "Golden", "openapi-v1.json");
        var expected = Canonical(JsonDocument.Parse(File.ReadAllText(goldenPath)).RootElement);

        Assert.True(string.Equals(expected, actual, StringComparison.Ordinal),
            "The generated OpenAPI document drifted from Golden/openapi-v1.json.");
    }

    [Theory]
    [InlineData("/api/v1/orders")]
    [InlineData("/api/v1/payments/initiate")]
    public async Task ValidationProblems_AreDocumentedAsProblemDetails(string path)
    {
        var root = await LoadDocumentAsync();

        var badRequest = root.GetProperty("paths").GetProperty(path)
            .GetProperty("post").GetProperty("responses").GetProperty("400");

        var media = badRequest.GetProperty("content").EnumerateObject().Single();
        Assert.Equal("application/problem+json", media.Name);
        Assert.Equal("#/components/schemas/HttpValidationProblemDetails",
            media.Value.GetProperty("schema").GetProperty("$ref").GetString());
    }

    [Fact]
    public async Task HttpValidationProblemDetails_SchemaHasExpectedProperties()
    {
        var root = await LoadDocumentAsync();

        var schema = root.GetProperty("components").GetProperty("schemas")
            .GetProperty("HttpValidationProblemDetails");
        Assert.Equal("object", schema.GetProperty("type").GetString());

        AssertSchemaProperties(root, "HttpValidationProblemDetails",
            "type", "title", "status", "detail", "instance", "errors");
    }

    [Fact]
    public async Task HealthSuccessResponse_DeclaresStringSchema()
    {
        var root = await LoadDocumentAsync();

        var schema = root.GetProperty("paths").GetProperty("/health")
            .GetProperty("get").GetProperty("responses").GetProperty("200")
            .GetProperty("content").GetProperty("application/json").GetProperty("schema");

        Assert.Equal("string", schema.GetProperty("type").GetString());
    }

    private static string Canonical(JsonElement element)
    {
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream, new JsonWriterOptions { Indented = true }))
        {
            WriteCanonical(writer, element);
        }

        return Encoding.UTF8.GetString(stream.ToArray());
    }

    private static void WriteCanonical(Utf8JsonWriter writer, JsonElement element)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                writer.WriteStartObject();
                foreach (var property in element.EnumerateObject().OrderBy(p => p.Name, StringComparer.Ordinal))
                {
                    writer.WritePropertyName(property.Name);
                    WriteCanonical(writer, property.Value);
                }
                writer.WriteEndObject();
                break;

            case JsonValueKind.Array:
                writer.WriteStartArray();
                foreach (var item in element.EnumerateArray())
                {
                    WriteCanonical(writer, item);
                }
                writer.WriteEndArray();
                break;

            default:
                element.WriteTo(writer);
                break;
        }
    }

    private static void AssertSchemaProperties(JsonElement root, string schemaName, params string[] expected)
    {
        var properties = root.GetProperty("components").GetProperty("schemas")
            .GetProperty(schemaName).GetProperty("properties");

        foreach (var name in expected)
        {
            Assert.True(properties.TryGetProperty(name, out _),
                $"{schemaName} schema must declare '{name}'.");
        }

        Assert.Equal(expected.Length, properties.EnumerateObject().Count());
    }

    private async Task<JsonElement> LoadDocumentAsync()
    {
        var response = await fixture.Client.GetAsync("/swagger/v1/swagger.json");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return document.RootElement.Clone();
    }
}
