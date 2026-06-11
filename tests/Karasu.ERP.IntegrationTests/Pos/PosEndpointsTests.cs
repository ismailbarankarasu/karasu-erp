using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Karasu.ERP.IntegrationTests.Infrastructure;
using Xunit;

namespace Karasu.ERP.IntegrationTests.Pos;

public class PosEndpointsTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public PosEndpointsTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Pos_full_sale_flow_should_work()
    {
        var slug = $"pos-{Guid.NewGuid():N}"[..18];
        var registerResponse = await _client.PostAsJsonAsync("/api/v1/auth/register", new
        {
            companyName = "POS Test Co",
            slug,
            email = $"{slug}@test.com",
            password = "Password123",
            fullName = "POS Tester"
        });

        registerResponse.EnsureSuccessStatusCode();
        var token = (await registerResponse.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("data").GetProperty("accessToken").GetString()!;

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var branchId = (await (await _client.GetAsync("/api/v1/branches")).Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("data")[0].GetProperty("id").GetGuid();

        var unitId = (await (await _client.GetAsync("/api/v1/units")).Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("data")[0].GetProperty("id").GetGuid();

        var sku = $"POS-{Guid.NewGuid():N}"[..12];
        var createProductResponse = await _client.PostAsJsonAsync("/api/v1/products", new
        {
            sku,
            barcode = $"869{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() % 1000000000000:D12}",
            name = "POS Test Ürün",
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
        var warehouseId = stockList.GetProperty("data").GetProperty("items")[0].GetProperty("warehouseId").GetGuid();

        await _client.PostAsJsonAsync("/api/v1/stock/adjust", new
        {
            warehouseId,
            productVariantId = variantId,
            quantityDelta = 20m,
            note = "POS test stok"
        }).ContinueWith(r => r.Result.EnsureSuccessStatusCode());

        var openSessionResponse = await _client.PostAsJsonAsync("/api/v1/pos/sessions/open", new
        {
            branchId,
            openingBalance = 100m
        });
        openSessionResponse.EnsureSuccessStatusCode();
        var sessionId = (await openSessionResponse.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("data").GetProperty("id").GetGuid();

        var currentSession = await _client.GetAsync("/api/v1/pos/sessions/current");
        currentSession.EnsureSuccessStatusCode();
        var currentJson = await currentSession.Content.ReadFromJsonAsync<JsonElement>();
        currentJson.GetProperty("data").GetProperty("id").GetGuid().Should().Be(sessionId);

        var searchResponse = await _client.GetAsync($"/api/v1/pos/products/search?search=POS");
        searchResponse.EnsureSuccessStatusCode();

        var saleResponse = await _client.PostAsJsonAsync("/api/v1/pos/sales", new
        {
            sessionId,
            customerId = (Guid?)null,
            lines = new[]
            {
                new
                {
                    productVariantId = variantId,
                    quantity = 2m,
                    unitPrice = 50m,
                    taxRate = 20m,
                    discount = 0m
                }
            },
            payments = new[]
            {
                new { method = 0, amount = 120m, changeAmount = 0m }
            }
        });

        saleResponse.EnsureSuccessStatusCode();
        var saleJson = await saleResponse.Content.ReadFromJsonAsync<JsonElement>();
        saleJson.GetProperty("data").GetProperty("grandTotal").GetDecimal().Should().Be(120m);

        var stockAfter = await (await _client.GetAsync("/api/v1/stock")).Content.ReadFromJsonAsync<JsonElement>();
        stockAfter.GetProperty("data").GetProperty("items")[0].GetProperty("quantity").GetDecimal().Should().Be(18m);

        var closeResponse = await _client.PostAsJsonAsync($"/api/v1/pos/sessions/{sessionId}/close", new
        {
            closingBalance = 220m
        });
        closeResponse.EnsureSuccessStatusCode();

        var afterClose = await _client.GetAsync("/api/v1/pos/sessions/current");
        afterClose.EnsureSuccessStatusCode();
        (await afterClose.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("data").ValueKind
            .Should().Be(JsonValueKind.Null);
    }

    [Fact]
    public async Task Pos_sale_should_fail_without_stock()
    {
        var slug = $"pos2-{Guid.NewGuid():N}"[..18];
        var registerResponse = await _client.PostAsJsonAsync("/api/v1/auth/register", new
        {
            companyName = "POS Fail Co",
            slug,
            email = $"{slug}@test.com",
            password = "Password123",
            fullName = "POS Fail Tester"
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
            sku = $"POSF-{Guid.NewGuid():N}"[..14],
            barcode = (string?)null,
            name = "POS Fail Ürün",
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

        var openSessionResponse = await _client.PostAsJsonAsync("/api/v1/pos/sessions/open", new
        {
            branchId,
            openingBalance = 0m
        });
        var sessionId = (await openSessionResponse.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("data").GetProperty("id").GetGuid();

        var saleResponse = await _client.PostAsJsonAsync("/api/v1/pos/sales", new
        {
            sessionId,
            customerId = (Guid?)null,
            lines = new[]
            {
                new
                {
                    productVariantId = variantId,
                    quantity = 1m,
                    unitPrice = 50m,
                    taxRate = 20m,
                    discount = 0m
                }
            },
            payments = new[] { new { method = 0, amount = 60m, changeAmount = 0m } }
        });

        saleResponse.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}
