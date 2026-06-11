using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Karasu.ERP.IntegrationTests.Infrastructure;
using Xunit;

namespace Karasu.ERP.IntegrationTests.Orders;

public class OrdersEndpointsTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public OrdersEndpointsTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Get_orders_should_require_authentication()
    {
        var response = await _client.GetAsync("/api/v1/orders");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Registered_user_should_create_confirm_and_cancel_order()
    {
        var slug = $"ord-{Guid.NewGuid():N}"[..18];
        var registerResponse = await _client.PostAsJsonAsync("/api/v1/auth/register", new
        {
            companyName = "Order Test Co",
            slug,
            email = $"{slug}@test.com",
            password = "Password123",
            fullName = "Order Tester"
        });

        registerResponse.EnsureSuccessStatusCode();
        var registerJson = await registerResponse.Content.ReadFromJsonAsync<JsonElement>();
        var token = registerJson.GetProperty("data").GetProperty("accessToken").GetString()!;

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var branchesResponse = await _client.GetAsync("/api/v1/branches");
        branchesResponse.EnsureSuccessStatusCode();
        var branchesJson = await branchesResponse.Content.ReadFromJsonAsync<JsonElement>();
        var branchId = branchesJson.GetProperty("data")[0].GetProperty("id").GetGuid();

        var unitsResponse = await _client.GetAsync("/api/v1/units");
        var unitsJson = await unitsResponse.Content.ReadFromJsonAsync<JsonElement>();
        var unitId = unitsJson.GetProperty("data")[0].GetProperty("id").GetGuid();

        var createProductResponse = await _client.PostAsJsonAsync("/api/v1/products", new
        {
            sku = "ORD-SKU-001",
            barcode = "8690000000100",
            name = "Sipariş Test Ürün",
            categoryId = (Guid?)null,
            brandId = (Guid?)null,
            unitId,
            purchasePrice = 40m,
            salePrice = 100m,
            taxRate = 20m,
            minStock = 1m
        });
        createProductResponse.EnsureSuccessStatusCode();
        var productId = (await createProductResponse.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("data").GetProperty("id").GetGuid();

        var productResponse = await _client.GetAsync($"/api/v1/products/{productId}");
        var productJson = await productResponse.Content.ReadFromJsonAsync<JsonElement>();
        var variantId = productJson.GetProperty("data").GetProperty("defaultVariantId").GetGuid();

        var stockList = await (await _client.GetAsync("/api/v1/stock")).Content.ReadFromJsonAsync<JsonElement>();
        var warehouseId = stockList.GetProperty("data").GetProperty("items")[0].GetProperty("warehouseId").GetGuid();

        var adjustResponse = await _client.PostAsJsonAsync("/api/v1/stock/adjust", new
        {
            warehouseId,
            productVariantId = variantId,
            quantityDelta = 10m,
            note = "Sipariş test stok"
        });
        adjustResponse.EnsureSuccessStatusCode();

        var createCustomerResponse = await _client.PostAsJsonAsync("/api/v1/customers", new
        {
            type = 0,
            fullName = "Sipariş Müşterisi",
            companyName = (string?)null,
            taxNumber = (string?)null,
            phone = "05321111111",
            email = "order-customer@test.com",
            address = "Adres",
            city = "İstanbul",
            creditLimit = 0m
        });
        createCustomerResponse.EnsureSuccessStatusCode();
        var customerId = (await createCustomerResponse.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("data").GetProperty("id").GetGuid();

        var createOrderResponse = await _client.PostAsJsonAsync("/api/v1/orders", new
        {
            branchId,
            customerId,
            notes = "Test sipariş",
            lines = new[]
            {
                new
                {
                    productVariantId = variantId,
                    quantity = 2m,
                    unitPrice = 100m,
                    taxRate = 20m,
                    discount = 0m
                }
            }
        });

        createOrderResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var orderId = (await createOrderResponse.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("data").GetProperty("id").GetGuid();

        var confirmResponse = await _client.PostAsync($"/api/v1/orders/{orderId}/confirm", null);
        confirmResponse.EnsureSuccessStatusCode();

        var getOrderResponse = await _client.GetAsync($"/api/v1/orders/{orderId}");
        getOrderResponse.EnsureSuccessStatusCode();
        var orderJson = await getOrderResponse.Content.ReadFromJsonAsync<JsonElement>();
        orderJson.GetProperty("data").GetProperty("status").GetInt32().Should().Be(2); // Confirmed

        var cancelResponse = await _client.PostAsJsonAsync($"/api/v1/orders/{orderId}/cancel", new { reason = "Test iptal" });
        cancelResponse.EnsureSuccessStatusCode();

        var listResponse = await _client.GetAsync("/api/v1/orders");
        listResponse.EnsureSuccessStatusCode();
        var listJson = await listResponse.Content.ReadFromJsonAsync<JsonElement>();
        listJson.GetProperty("data").GetProperty("items").GetArrayLength().Should().Be(1);
    }
}
