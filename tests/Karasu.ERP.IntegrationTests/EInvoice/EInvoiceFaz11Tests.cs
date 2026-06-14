using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Karasu.ERP.IntegrationTests.Infrastructure;
using Xunit;

namespace Karasu.ERP.IntegrationTests.EInvoice;

public class EInvoiceFaz11Tests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public EInvoiceFaz11Tests(CustomWebApplicationFactory factory) => _client = factory.CreateClient();

    [Fact]
    public async Task EInvoice_profile_submit_and_dispatch_should_work()
    {
        var ctx = await SetupAsync("f11einv");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", ctx.Token);

        var profileResponse = await _client.PutAsJsonAsync("/api/v1/einvoice/profile", new
        {
            provider = 0,
            taxNumber = "1234567890",
            companyTitle = "Faz11 Test A.Ş.",
            isActive = true
        });
        profileResponse.EnsureSuccessStatusCode();

        var profile = await (await _client.GetAsync("/api/v1/einvoice/profile")).Content.ReadFromJsonAsync<JsonElement>();
        profile.GetProperty("data").GetProperty("isConfigured").GetBoolean().Should().BeTrue();

        await _client.PostAsJsonAsync("/api/v1/stock/adjust", new
        {
            warehouseId = ctx.WarehouseId,
            productVariantId = ctx.VariantId,
            quantityDelta = 20m,
            note = "einvoice test"
        }).ContinueWith(r => r.Result.EnsureSuccessStatusCode());

        var orderResponse = await _client.PostAsJsonAsync("/api/v1/orders", new
        {
            branchId = ctx.BranchId,
            customerId = ctx.CustomerId,
            lines = new[]
            {
                new { productVariantId = ctx.VariantId, quantity = 2m, unitPrice = 100m, taxRate = 20m, discount = 0m }
            }
        });
        orderResponse.EnsureSuccessStatusCode();
        var orderId = (await orderResponse.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("data").GetProperty("id").GetGuid();

        await _client.PostAsync($"/api/v1/orders/{orderId}/confirm", null)
            .ContinueWith(r => r.Result.EnsureSuccessStatusCode());

        var invoiceResponse = await _client.PostAsJsonAsync($"/api/v1/orders/{orderId}/invoice", new
        {
            type = 0,
            issueImmediately = true
        });
        invoiceResponse.EnsureSuccessStatusCode();
        var invoiceId = (await invoiceResponse.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("data").GetProperty("id").GetGuid();

        var submitResponse = await _client.PostAsync($"/api/v1/einvoice/submit/{invoiceId}", null);
        submitResponse.EnsureSuccessStatusCode();

        var submissions = await (await _client.GetAsync("/api/v1/einvoice/submissions")).Content.ReadFromJsonAsync<JsonElement>();
        submissions.GetProperty("data").GetProperty("items").GetArrayLength().Should().BeGreaterThan(0);

        var dispatchResponse = await _client.PostAsync($"/api/v1/einvoice/dispatch/{orderId}", null);
        dispatchResponse.EnsureSuccessStatusCode();
    }

    private async Task<TestContext> SetupAsync(string prefix)
    {
        var slug = $"{prefix}-{Guid.NewGuid():N}"[..18];
        var registerResponse = await _client.PostAsJsonAsync("/api/v1/auth/register", new
        {
            companyName = "Faz11 EInvoice Co",
            slug,
            email = $"{slug}@test.com",
            password = "Password123",
            fullName = "Faz11 Tester"
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

        var customerId = (await (await _client.PostAsJsonAsync("/api/v1/customers", new
        {
            type = 0,
            fullName = "Faz11 Müşteri",
            phone = "05325555555",
            email = $"{prefix}@cust.com",
            creditLimit = 0m
        })).Content.ReadFromJsonAsync<JsonElement>()).GetProperty("data").GetProperty("id").GetGuid();

        return new TestContext(token, branchId, variantId, warehouseId, customerId);
    }

    private record TestContext(string Token, Guid BranchId, Guid VariantId, Guid WarehouseId, Guid CustomerId);
}
