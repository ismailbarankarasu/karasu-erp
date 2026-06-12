using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Karasu.ERP.IntegrationTests.Infrastructure;
using Xunit;

namespace Karasu.ERP.IntegrationTests.Orders;

public class OrdersFaz3Tests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public OrdersFaz3Tests(CustomWebApplicationFactory factory) => _client = factory.CreateClient();

    [Fact]
    public async Task Order_update_history_and_status_flow_should_work()
    {
        var ctx = await SetupAsync("f3ord");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", ctx.Token);

        var createResponse = await _client.PostAsJsonAsync("/api/v1/orders", new
        {
            branchId = ctx.BranchId,
            customerId = ctx.CustomerId,
            notes = "İlk taslak",
            lines = new[]
            {
                new { productVariantId = ctx.VariantId, quantity = 1m, unitPrice = 100m, taxRate = 20m, discount = 0m }
            }
        });
        createResponse.EnsureSuccessStatusCode();
        var orderId = (await createResponse.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("data").GetProperty("id").GetGuid();

        var updateResponse = await _client.PutAsJsonAsync($"/api/v1/orders/{orderId}", new
        {
            customerId = ctx.CustomerId,
            notes = "Güncellenmiş taslak",
            lines = new[]
            {
                new { productVariantId = ctx.VariantId, quantity = 3m, unitPrice = 100m, taxRate = 20m, discount = 0m }
            }
        });
        updateResponse.EnsureSuccessStatusCode();

        var detail = await (await _client.GetAsync($"/api/v1/orders/{orderId}")).Content.ReadFromJsonAsync<JsonElement>();
        detail.GetProperty("data").GetProperty("grandTotal").GetDecimal().Should().Be(360m);

        await _client.PostAsJsonAsync("/api/v1/stock/adjust", new
        {
            warehouseId = ctx.WarehouseId,
            productVariantId = ctx.VariantId,
            quantityDelta = 10m,
            note = "faz3 sipariş onay stok"
        }).ContinueWith(r => r.Result.EnsureSuccessStatusCode());

        await _client.PostAsync($"/api/v1/orders/{orderId}/confirm", null).ContinueWith(r => r.Result.EnsureSuccessStatusCode());

        var history = await (await _client.GetAsync($"/api/v1/orders/{orderId}/history")).Content.ReadFromJsonAsync<JsonElement>();
        history.GetProperty("data").GetArrayLength().Should().BeGreaterThan(0);

        var statusResponse = await _client.PatchAsJsonAsync($"/api/v1/orders/{orderId}/status", new
        {
            status = 3,
            note = "Hazırlanıyor"
        });
        statusResponse.EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task Quote_convert_and_invoice_flow_should_work()
    {
        var ctx = await SetupAsync("f3qt");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", ctx.Token);

        var quoteResponse = await _client.PostAsJsonAsync("/api/v1/quotes", new
        {
            branchId = ctx.BranchId,
            customerId = ctx.CustomerId,
            notes = "Teklif notu",
            validUntil = DateTime.UtcNow.AddDays(30),
            lines = new[]
            {
                new { productVariantId = ctx.VariantId, quantity = 2m, unitPrice = 100m, taxRate = 20m, discount = 0m }
            }
        });
        quoteResponse.EnsureSuccessStatusCode();
        var quoteId = (await quoteResponse.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("data").GetProperty("id").GetGuid();

        var convertResponse = await _client.PostAsJsonAsync($"/api/v1/quotes/{quoteId}/convert", new
        {
            branchId = ctx.BranchId
        });
        convertResponse.EnsureSuccessStatusCode();
        var orderId = (await convertResponse.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("data").GetProperty("orderId").GetGuid();

        await _client.PostAsJsonAsync("/api/v1/stock/adjust", new
        {
            warehouseId = ctx.WarehouseId,
            productVariantId = ctx.VariantId,
            quantityDelta = 10m,
            note = "fatura test stok"
        }).ContinueWith(r => r.Result.EnsureSuccessStatusCode());

        await _client.PostAsync($"/api/v1/orders/{orderId}/confirm", null).ContinueWith(r => r.Result.EnsureSuccessStatusCode());

        var invoiceResponse = await _client.PostAsJsonAsync($"/api/v1/orders/{orderId}/invoice", new
        {
            type = 0,
            issueImmediately = true
        });
        invoiceResponse.EnsureSuccessStatusCode();
        var invoiceId = (await invoiceResponse.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("data").GetProperty("id").GetGuid();

        var invoiceDetail = await _client.GetAsync($"/api/v1/invoices/{invoiceId}");
        invoiceDetail.EnsureSuccessStatusCode();
        var invoiceJson = await invoiceDetail.Content.ReadFromJsonAsync<JsonElement>();
        invoiceJson.GetProperty("data").GetProperty("status").GetInt32().Should().Be(1);

        var quoteDetail = await (await _client.GetAsync($"/api/v1/quotes/{quoteId}")).Content.ReadFromJsonAsync<JsonElement>();
        quoteDetail.GetProperty("data").GetProperty("status").GetInt32().Should().Be(4);
    }

    [Fact]
    public async Task Draft_order_should_be_deletable()
    {
        var ctx = await SetupAsync("f3del");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", ctx.Token);

        var createResponse = await _client.PostAsJsonAsync("/api/v1/orders", new
        {
            branchId = ctx.BranchId,
            customerId = ctx.CustomerId,
            lines = new[]
            {
                new { productVariantId = ctx.VariantId, quantity = 1m, unitPrice = 50m, taxRate = 20m, discount = 0m }
            }
        });
        var orderId = (await createResponse.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("data").GetProperty("id").GetGuid();

        var deleteResponse = await _client.DeleteAsync($"/api/v1/orders/{orderId}");
        deleteResponse.EnsureSuccessStatusCode();

        var getResponse = await _client.GetAsync($"/api/v1/orders/{orderId}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    private async Task<TestContext> SetupAsync(string prefix)
    {
        var slug = $"{prefix}-{Guid.NewGuid():N}"[..18];
        var registerResponse = await _client.PostAsJsonAsync("/api/v1/auth/register", new
        {
            companyName = "Faz3 Order Co",
            slug,
            email = $"{slug}@test.com",
            password = "Password123",
            fullName = "Faz3 Tester"
        });
        registerResponse.EnsureSuccessStatusCode();

        var token = (await registerResponse.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("data").GetProperty("accessToken").GetString()!;

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var branchId = (await (await _client.GetAsync("/api/v1/branches")).Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("data")[0].GetProperty("id").GetGuid();
        var unitId = (await (await _client.GetAsync("/api/v1/units")).Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("data")[0].GetProperty("id").GetGuid();

        var productResponse = await _client.PostAsJsonAsync("/api/v1/products", new
        {
            sku = $"{prefix.ToUpper()}-{Guid.NewGuid():N}"[..12],
            barcode = (string?)null,
            name = $"{prefix} Ürün",
            unitId,
            purchasePrice = 10m,
            salePrice = 100m,
            taxRate = 20m,
            minStock = 1m
        });
        productResponse.EnsureSuccessStatusCode();
        var productId = (await productResponse.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("data").GetProperty("id").GetGuid();
        var variantId = (await (await _client.GetAsync($"/api/v1/products/{productId}")).Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("data").GetProperty("defaultVariantId").GetGuid();

        var warehouseId = (await (await _client.GetAsync("/api/v1/stock")).Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("data").GetProperty("items")[0].GetProperty("warehouseId").GetGuid();

        var customerId = (await (await _client.PostAsJsonAsync("/api/v1/customers", new
        {
            type = 0,
            fullName = "Faz3 Müşteri",
            phone = "05324444444",
            email = $"{prefix}@cust.com",
            creditLimit = 0m
        })).Content.ReadFromJsonAsync<JsonElement>()).GetProperty("data").GetProperty("id").GetGuid();

        return new TestContext(token, branchId, variantId, warehouseId, customerId);
    }

    private record TestContext(string Token, Guid BranchId, Guid VariantId, Guid WarehouseId, Guid CustomerId);
}
