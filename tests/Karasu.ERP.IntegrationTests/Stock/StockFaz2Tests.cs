using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Karasu.ERP.IntegrationTests.Infrastructure;
using Xunit;

namespace Karasu.ERP.IntegrationTests.Stock;

public class StockFaz2Tests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public StockFaz2Tests(CustomWebApplicationFactory factory) => _client = factory.CreateClient();

    [Fact]
    public async Task Warehouse_crud_and_transfer_should_work()
    {
        var ctx = await RegisterAndSetupProductAsync("wh");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", ctx.Token);

        var warehouses = await (await _client.GetAsync("/api/v1/warehouses")).Content.ReadFromJsonAsync<JsonElement>();
        warehouses.GetProperty("data").GetArrayLength().Should().BeGreaterThan(0);
        var defaultWarehouseId = warehouses.GetProperty("data")[0].GetProperty("id").GetGuid();

        var createWarehouseResponse = await _client.PostAsJsonAsync("/api/v1/warehouses", new
        {
            branchId = ctx.BranchId,
            name = "Yan Depo",
            code = $"WH-{Guid.NewGuid():N}"[..8],
            isDefault = false
        });
        createWarehouseResponse.EnsureSuccessStatusCode();
        var secondWarehouseId = (await createWarehouseResponse.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("data").GetProperty("id").GetGuid();

        await _client.PostAsJsonAsync("/api/v1/stock/adjust", new
        {
            warehouseId = defaultWarehouseId,
            productVariantId = ctx.VariantId,
            quantityDelta = 20m,
            note = "transfer kaynak"
        }).ContinueWith(r => r.Result.EnsureSuccessStatusCode());

        var transferResponse = await _client.PostAsJsonAsync("/api/v1/stock/transfers", new
        {
            fromWarehouseId = defaultWarehouseId,
            toWarehouseId = secondWarehouseId,
            note = "Test transfer",
            lines = new[]
            {
                new { productVariantId = ctx.VariantId, quantity = 8m }
            }
        });
        transferResponse.EnsureSuccessStatusCode();
        var transferId = (await transferResponse.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("data").GetProperty("id").GetGuid();

        var completeResponse = await _client.PatchAsync($"/api/v1/stock/transfers/{transferId}/complete", null);
        completeResponse.EnsureSuccessStatusCode();

        var sourceStock = await (await _client.GetAsync($"/api/v1/stock?warehouseId={defaultWarehouseId}"))
            .Content.ReadFromJsonAsync<JsonElement>();
        sourceStock.GetProperty("data").GetProperty("items")[0].GetProperty("quantity").GetDecimal().Should().Be(12m);

        var destStock = await (await _client.GetAsync($"/api/v1/stock?warehouseId={secondWarehouseId}"))
            .Content.ReadFromJsonAsync<JsonElement>();
        destStock.GetProperty("data").GetProperty("items")[0].GetProperty("quantity").GetDecimal().Should().Be(8m);

        var updateResponse = await _client.PutAsJsonAsync($"/api/v1/warehouses/{secondWarehouseId}", new
        {
            name = "Yan Depo Güncel",
            code = $"WH2-{Guid.NewGuid():N}"[..8],
            isDefault = false
        });
        updateResponse.EnsureSuccessStatusCode();

        var deleteDefaultResponse = await _client.DeleteAsync($"/api/v1/warehouses/{defaultWarehouseId}");
        deleteDefaultResponse.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Stock_count_should_adjust_inventory()
    {
        var ctx = await RegisterAndSetupProductAsync("cnt");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", ctx.Token);

        var warehouseId = (await (await _client.GetAsync("/api/v1/warehouses")).Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("data")[0].GetProperty("id").GetGuid();

        await _client.PostAsJsonAsync("/api/v1/stock/adjust", new
        {
            warehouseId,
            productVariantId = ctx.VariantId,
            quantityDelta = 15m,
            note = "sayım öncesi"
        }).ContinueWith(r => r.Result.EnsureSuccessStatusCode());

        var countResponse = await _client.PostAsJsonAsync("/api/v1/stock/counts", new
        {
            warehouseId,
            note = "Aylık sayım"
        });
        countResponse.EnsureSuccessStatusCode();
        var countId = (await countResponse.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("data").GetProperty("id").GetGuid();

        var countDetail = await (await _client.GetAsync($"/api/v1/stock/counts/{countId}")).Content.ReadFromJsonAsync<JsonElement>();
        var lineId = countDetail.GetProperty("data").GetProperty("lines")[0].GetProperty("id").GetGuid();

        var updateLinesResponse = await _client.PutAsJsonAsync($"/api/v1/stock/counts/{countId}/lines", new
        {
            lines = new[]
            {
                new { lineId, productVariantId = (Guid?)null, countedQty = 12m }
            }
        });
        updateLinesResponse.EnsureSuccessStatusCode();

        var completeResponse = await _client.PostAsync($"/api/v1/stock/counts/{countId}/complete", null);
        completeResponse.EnsureSuccessStatusCode();

        var stockAfter = await (await _client.GetAsync($"/api/v1/stock?warehouseId={warehouseId}"))
            .Content.ReadFromJsonAsync<JsonElement>();
        stockAfter.GetProperty("data").GetProperty("items")[0].GetProperty("quantity").GetDecimal().Should().Be(12m);
    }

    [Fact]
    public async Task Stock_alerts_should_return_critical_items()
    {
        var ctx = await RegisterAndSetupProductAsync("alt");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", ctx.Token);

        var warehouseId = (await (await _client.GetAsync("/api/v1/warehouses")).Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("data")[0].GetProperty("id").GetGuid();

        await _client.PostAsJsonAsync("/api/v1/stock/adjust", new
        {
            warehouseId,
            productVariantId = ctx.VariantId,
            quantityDelta = 2m,
            note = "kritik stok test"
        }).ContinueWith(r => r.Result.EnsureSuccessStatusCode());

        var alerts = await (await _client.GetAsync("/api/v1/stock/alerts")).Content.ReadFromJsonAsync<JsonElement>();
        alerts.GetProperty("data").GetArrayLength().Should().BeGreaterThan(0);
        alerts.GetProperty("data")[0].GetProperty("quantity").GetDecimal().Should().BeLessOrEqualTo(
            alerts.GetProperty("data")[0].GetProperty("minStock").GetDecimal());

        var byVariant = await _client.GetAsync($"/api/v1/stock/{ctx.VariantId}");
        byVariant.EnsureSuccessStatusCode();
        var variantJson = await byVariant.Content.ReadFromJsonAsync<JsonElement>();
        variantJson.GetProperty("data").GetProperty("totalQuantity").GetDecimal().Should().Be(2m);
    }

    private async Task<TestContext> RegisterAndSetupProductAsync(string prefix)
    {
        var slug = $"{prefix}-{Guid.NewGuid():N}"[..18];
        var registerResponse = await _client.PostAsJsonAsync("/api/v1/auth/register", new
        {
            companyName = "Stock Faz2 Co",
            slug,
            email = $"{slug}@test.com",
            password = "Password123",
            fullName = "Stock Faz2 Tester"
        });
        registerResponse.EnsureSuccessStatusCode();

        var token = (await registerResponse.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("data").GetProperty("accessToken").GetString()!;

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var branchId = (await (await _client.GetAsync("/api/v1/branches")).Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("data")[0].GetProperty("id").GetGuid();

        var unitId = (await (await _client.GetAsync("/api/v1/units")).Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("data")[0].GetProperty("id").GetGuid();

        var sku = $"{prefix.ToUpper()}-{Guid.NewGuid():N}"[..12];
        var createProductResponse = await _client.PostAsJsonAsync("/api/v1/products", new
        {
            sku,
            barcode = (string?)null,
            name = $"{prefix} Ürün",
            unitId,
            purchasePrice = 10m,
            salePrice = 50m,
            taxRate = 20m,
            minStock = 5m
        });
        createProductResponse.EnsureSuccessStatusCode();

        var productId = (await createProductResponse.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("data").GetProperty("id").GetGuid();

        var variantId = (await (await _client.GetAsync($"/api/v1/products/{productId}")).Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("data").GetProperty("defaultVariantId").GetGuid();

        return new TestContext(token, branchId, variantId);
    }

    private record TestContext(string Token, Guid BranchId, Guid VariantId);
}
