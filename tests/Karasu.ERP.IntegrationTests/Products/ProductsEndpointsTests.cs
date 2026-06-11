using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Karasu.ERP.IntegrationTests.Infrastructure;
using Xunit;

namespace Karasu.ERP.IntegrationTests.Products;

public class ProductsEndpointsTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public ProductsEndpointsTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Get_products_should_require_authentication()
    {
        var response = await _client.GetAsync("/api/v1/products");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Registered_user_should_create_and_list_products()
    {
        var slug = $"prod-{Guid.NewGuid():N}"[..18];
        var registerResponse = await _client.PostAsJsonAsync("/api/v1/auth/register", new
        {
            companyName = "Product Test Co",
            slug,
            email = $"{slug}@test.com",
            password = "Password123",
            fullName = "Product Tester"
        });

        registerResponse.EnsureSuccessStatusCode();
        var registerJson = await registerResponse.Content.ReadFromJsonAsync<JsonElement>();
        var token = registerJson.GetProperty("data").GetProperty("accessToken").GetString()!;

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var unitsResponse = await _client.GetAsync("/api/v1/units");
        unitsResponse.EnsureSuccessStatusCode();
        var unitsJson = await unitsResponse.Content.ReadFromJsonAsync<JsonElement>();
        var unitId = unitsJson.GetProperty("data")[0].GetProperty("id").GetGuid();

        var createResponse = await _client.PostAsJsonAsync("/api/v1/products", new
        {
            sku = "SKU-001",
            barcode = "8690000000001",
            name = "Test Ürün",
            categoryId = (Guid?)null,
            brandId = (Guid?)null,
            unitId,
            purchasePrice = 50m,
            salePrice = 99.90m,
            taxRate = 20m,
            minStock = 5m
        });

        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var listResponse = await _client.GetAsync("/api/v1/products");
        listResponse.EnsureSuccessStatusCode();
        var listJson = await listResponse.Content.ReadFromJsonAsync<JsonElement>();
        listJson.GetProperty("data").GetProperty("items").GetArrayLength().Should().Be(1);
    }
}
