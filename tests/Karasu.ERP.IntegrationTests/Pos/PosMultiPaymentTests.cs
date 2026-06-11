using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Karasu.ERP.IntegrationTests.Infrastructure;
using Xunit;

namespace Karasu.ERP.IntegrationTests.Pos;

public class PosMultiPaymentTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public PosMultiPaymentTests(CustomWebApplicationFactory factory) => _client = factory.CreateClient();

    [Fact]
    public async Task Pos_split_payment_with_change_should_work()
    {
        var slug = $"posmp-{Guid.NewGuid():N}"[..18];
        var registerResponse = await _client.PostAsJsonAsync("/api/v1/auth/register", new
        {
            companyName = "Multi Pay Co",
            slug,
            email = $"{slug}@test.com",
            password = "Password123",
            fullName = "Multi Pay Tester"
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
            sku = $"MP-{Guid.NewGuid():N}"[..12],
            barcode = (string?)null,
            name = "Multi Pay Ürün",
            unitId,
            purchasePrice = 10m,
            salePrice = 100m,
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
            quantityDelta = 5m,
            note = "multi pay stok"
        }).ContinueWith(r => r.Result.EnsureSuccessStatusCode());

        var sessionId = (await (await _client.PostAsJsonAsync("/api/v1/pos/sessions/open", new
        {
            branchId,
            openingBalance = 50m
        })).Content.ReadFromJsonAsync<JsonElement>()).GetProperty("data").GetProperty("id").GetGuid();

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
                    unitPrice = 100m,
                    taxRate = 20m,
                    discount = 0m
                }
            },
            payments = new object[]
            {
                new { method = 0, amount = 50m, tenderedAmount = 60m },
                new { method = 1, amount = 70m }
            }
        });

        saleResponse.EnsureSuccessStatusCode();
        var saleJson = await saleResponse.Content.ReadFromJsonAsync<JsonElement>();
        saleJson.GetProperty("data").GetProperty("grandTotal").GetDecimal().Should().Be(120m);

        var payments = saleJson.GetProperty("data").GetProperty("payments");
        payments.GetArrayLength().Should().Be(2);
        payments[0].GetProperty("changeAmount").GetDecimal().Should().Be(10m);
    }

    [Fact]
    public async Task Pos_credit_payment_should_require_customer()
    {
        var slug = $"poscr-{Guid.NewGuid():N}"[..18];
        var registerResponse = await _client.PostAsJsonAsync("/api/v1/auth/register", new
        {
            companyName = "Credit Pay Co",
            slug,
            email = $"{slug}@test.com",
            password = "Password123",
            fullName = "Credit Pay Tester"
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
            sku = $"CR-{Guid.NewGuid():N}"[..12],
            barcode = (string?)null,
            name = "Credit Pay Ürün",
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
            quantityDelta = 2m,
            note = "credit pay stok"
        }).ContinueWith(r => r.Result.EnsureSuccessStatusCode());

        var sessionId = (await (await _client.PostAsJsonAsync("/api/v1/pos/sessions/open", new
        {
            branchId,
            openingBalance = 0m
        })).Content.ReadFromJsonAsync<JsonElement>()).GetProperty("data").GetProperty("id").GetGuid();

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
            payments = new[] { new { method = 3, amount = 60m } }
        });

        saleResponse.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}
