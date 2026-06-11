using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Karasu.ERP.IntegrationTests.Infrastructure;
using Xunit;

namespace Karasu.ERP.IntegrationTests.Stock;

public class StockEndpointsTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public StockEndpointsTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Order_confirm_should_deduct_stock_and_cancel_should_restore()
    {
        var slug = $"stk-{Guid.NewGuid():N}"[..18];
        var registerResponse = await _client.PostAsJsonAsync("/api/v1/auth/register", new
        {
            companyName = "Stock Test Co",
            slug,
            email = $"{slug}@test.com",
            password = "Password123",
            fullName = "Stock Tester"
        });

        registerResponse.EnsureSuccessStatusCode();
        var token = (await registerResponse.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("data").GetProperty("accessToken").GetString()!;

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var branchId = (await (await _client.GetAsync("/api/v1/branches")).Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("data")[0].GetProperty("id").GetGuid();

        var unitId = (await (await _client.GetAsync("/api/v1/units")).Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("data")[0].GetProperty("id").GetGuid();

        var createProductResponse = await _client.PostAsJsonAsync("/api/v1/products", new
        {
            sku = "STK-SKU-001",
            barcode = "8690000000200",
            name = "Stok Test Ürün",
            categoryId = (Guid?)null,
            brandId = (Guid?)null,
            unitId,
            purchasePrice = 10m,
            salePrice = 50m,
            taxRate = 20m,
            minStock = 1m
        });
        createProductResponse.EnsureSuccessStatusCode();

        var productId = (await createProductResponse.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("data").GetProperty("id").GetGuid();

        var variantId = (await (await _client.GetAsync($"/api/v1/products/{productId}")).Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("data").GetProperty("defaultVariantId").GetGuid();

        var stockList = await (await _client.GetAsync("/api/v1/stock")).Content.ReadFromJsonAsync<JsonElement>();
        var stockItem = stockList.GetProperty("data").GetProperty("items")[0];
        var warehouseId = stockItem.GetProperty("warehouseId").GetGuid();
        stockItem.GetProperty("quantity").GetDecimal().Should().Be(0);

        var adjustResponse = await _client.PostAsJsonAsync("/api/v1/stock/adjust", new
        {
            warehouseId,
            productVariantId = variantId,
            quantityDelta = 10m,
            note = "Test stok girişi"
        });
        adjustResponse.EnsureSuccessStatusCode();

        var customerId = (await (await _client.PostAsJsonAsync("/api/v1/customers", new
        {
            type = 0,
            fullName = "Stok Müşteri",
            companyName = (string?)null,
            taxNumber = (string?)null,
            phone = "05322222222",
            email = "stock-customer@test.com",
            address = "Adres",
            city = "İstanbul",
            creditLimit = 0m
        })).Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("data").GetProperty("id").GetGuid();

        var createOrderResponse = await _client.PostAsJsonAsync("/api/v1/orders", new
        {
            branchId,
            customerId,
            notes = "Stok test sipariş",
            lines = new[]
            {
                new
                {
                    productVariantId = variantId,
                    quantity = 3m,
                    unitPrice = 50m,
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

        stockList = await (await _client.GetAsync("/api/v1/stock")).Content.ReadFromJsonAsync<JsonElement>();
        stockList.GetProperty("data").GetProperty("items")[0].GetProperty("quantity").GetDecimal().Should().Be(7);

        var movements = await (await _client.GetAsync("/api/v1/stock/movements")).Content.ReadFromJsonAsync<JsonElement>();
        movements.GetProperty("data").GetProperty("items").GetArrayLength().Should().BeGreaterThan(0);

        var cancelResponse = await _client.PostAsJsonAsync($"/api/v1/orders/{orderId}/cancel", new { reason = "Stok test iptal" });
        cancelResponse.EnsureSuccessStatusCode();

        stockList = await (await _client.GetAsync("/api/v1/stock")).Content.ReadFromJsonAsync<JsonElement>();
        stockList.GetProperty("data").GetProperty("items")[0].GetProperty("quantity").GetDecimal().Should().Be(10);
    }

    [Fact]
    public async Task Order_confirm_should_fail_when_insufficient_stock()
    {
        var slug = $"stk2-{Guid.NewGuid():N}"[..18];
        var registerResponse = await _client.PostAsJsonAsync("/api/v1/auth/register", new
        {
            companyName = "Stock Fail Co",
            slug,
            email = $"{slug}@test.com",
            password = "Password123",
            fullName = "Stock Fail Tester"
        });

        registerResponse.EnsureSuccessStatusCode();
        var token = (await registerResponse.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("data").GetProperty("accessToken").GetString()!;

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var branchId = (await (await _client.GetAsync("/api/v1/branches")).Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("data")[0].GetProperty("id").GetGuid();

        var unitId = (await (await _client.GetAsync("/api/v1/units")).Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("data")[0].GetProperty("id").GetGuid();

        var createProductResponse = await _client.PostAsJsonAsync("/api/v1/products", new
        {
            sku = "STK-SKU-002",
            barcode = "8690000000201",
            name = "Stok Fail Ürün",
            categoryId = (Guid?)null,
            brandId = (Guid?)null,
            unitId,
            purchasePrice = 10m,
            salePrice = 50m,
            taxRate = 20m,
            minStock = 1m
        });
        createProductResponse.EnsureSuccessStatusCode();

        var productId = (await createProductResponse.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("data").GetProperty("id").GetGuid();

        var variantId = (await (await _client.GetAsync($"/api/v1/products/{productId}")).Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("data").GetProperty("defaultVariantId").GetGuid();

        var customerId = (await (await _client.PostAsJsonAsync("/api/v1/customers", new
        {
            type = 0,
            fullName = "Stok Fail Müşteri",
            companyName = (string?)null,
            taxNumber = (string?)null,
            phone = "05323333333",
            email = "stock-fail@test.com",
            address = "Adres",
            city = "İstanbul",
            creditLimit = 0m
        })).Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("data").GetProperty("id").GetGuid();

        var createOrderResponse = await _client.PostAsJsonAsync("/api/v1/orders", new
        {
            branchId,
            customerId,
            lines = new[]
            {
                new
                {
                    productVariantId = variantId,
                    quantity = 5m,
                    unitPrice = 50m,
                    taxRate = 20m,
                    discount = 0m
                }
            }
        });
        var orderId = (await createOrderResponse.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("data").GetProperty("id").GetGuid();

        var confirmResponse = await _client.PostAsync($"/api/v1/orders/{orderId}/confirm", null);
        confirmResponse.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var errorJson = await confirmResponse.Content.ReadFromJsonAsync<JsonElement>();
        errorJson.GetProperty("errors")[0].GetProperty("code").GetString().Should().Be("INSUFFICIENT_STOCK");
    }
}
