using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Karasu.ERP.IntegrationTests.Infrastructure;
using Xunit;

namespace Karasu.ERP.IntegrationTests.Suppliers;

public class SuppliersFaz10Tests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public SuppliersFaz10Tests(CustomWebApplicationFactory factory) => _client = factory.CreateClient();

    [Fact]
    public async Task Supplier_purchase_order_and_performance_should_work()
    {
        var ctx = await SetupAsync("f10sup");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", ctx.Token);

        var supplierResponse = await _client.PostAsJsonAsync("/api/v1/suppliers", new
        {
            name = "Test Tedarikçi A.Ş.",
            taxNumber = $"T{Guid.NewGuid():N}"[..10],
            contactPerson = "Ali Veli",
            phone = "05321112233",
            email = "supplier@test.com"
        });
        supplierResponse.EnsureSuccessStatusCode();
        var supplierId = (await supplierResponse.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("data").GetProperty("id").GetGuid();

        var poResponse = await _client.PostAsJsonAsync("/api/v1/purchase-orders", new
        {
            supplierId,
            expectedDate = DateTime.UtcNow.AddDays(7).ToString("yyyy-MM-dd"),
            lines = new[]
            {
                new { productVariantId = ctx.VariantId, quantity = 10m, unitPrice = 50m, taxRate = 20m }
            }
        });
        poResponse.EnsureSuccessStatusCode();
        var poId = (await poResponse.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("data").GetProperty("id").GetGuid();

        var poList = await (await _client.GetAsync("/api/v1/purchase-orders")).Content.ReadFromJsonAsync<JsonElement>();
        poList.GetProperty("data").GetProperty("items").GetArrayLength().Should().BeGreaterThan(0);

        var poDetail = await (await _client.GetAsync($"/api/v1/purchase-orders/{poId}")).Content.ReadFromJsonAsync<JsonElement>();
        var lineId = poDetail.GetProperty("data").GetProperty("lines")[0].GetProperty("id").GetGuid();

        var receiveResponse = await _client.PatchAsJsonAsync($"/api/v1/purchase-orders/{poId}/receive", new
        {
            warehouseId = ctx.WarehouseId,
            lines = new[] { new { lineId, quantity = 10m } }
        });
        receiveResponse.EnsureSuccessStatusCode();

        var performance = await (await _client.GetAsync($"/api/v1/suppliers/{supplierId}/performance"))
            .Content.ReadFromJsonAsync<JsonElement>();
        performance.GetProperty("data").GetProperty("totalOrders").GetInt32().Should().BeGreaterThan(0);
        performance.GetProperty("data").GetProperty("completedOrders").GetInt32().Should().Be(1);
    }

    private async Task<TestContext> SetupAsync(string prefix)
    {
        var slug = $"{prefix}-{Guid.NewGuid():N}"[..18];
        var registerResponse = await _client.PostAsJsonAsync("/api/v1/auth/register", new
        {
            companyName = "Faz10 Supplier Co",
            slug,
            email = $"{slug}@test.com",
            password = "Password123",
            fullName = "Faz10 Tester"
        });
        registerResponse.EnsureSuccessStatusCode();

        var token = (await registerResponse.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("data").GetProperty("accessToken").GetString()!;

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var unitId = (await (await _client.GetAsync("/api/v1/units")).Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("data")[0].GetProperty("id").GetGuid();

        var productResponse = await _client.PostAsJsonAsync("/api/v1/products", new
        {
            sku = $"{prefix.ToUpper()}-{Guid.NewGuid():N}"[..12],
            name = $"{prefix} Ürün",
            unitId,
            purchasePrice = 40m,
            salePrice = 100m,
            taxRate = 20m,
            minStock = 5m
        });
        productResponse.EnsureSuccessStatusCode();
        var productId = (await productResponse.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("data").GetProperty("id").GetGuid();
        var variantId = (await (await _client.GetAsync($"/api/v1/products/{productId}")).Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("data").GetProperty("defaultVariantId").GetGuid();

        var warehouseId = (await (await _client.GetAsync("/api/v1/stock")).Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("data").GetProperty("items")[0].GetProperty("warehouseId").GetGuid();

        return new TestContext(token, variantId, warehouseId);
    }

    private record TestContext(string Token, Guid VariantId, Guid WarehouseId);
}
