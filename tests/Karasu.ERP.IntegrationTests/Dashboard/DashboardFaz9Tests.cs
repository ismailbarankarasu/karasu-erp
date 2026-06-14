using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Karasu.ERP.IntegrationTests.Infrastructure;
using Xunit;

namespace Karasu.ERP.IntegrationTests.Dashboard;

public class DashboardFaz9Tests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public DashboardFaz9Tests(CustomWebApplicationFactory factory) => _client = factory.CreateClient();

    [Fact]
    public async Task Dashboard_summary_and_trends_should_work()
    {
        var ctx = await SetupAsync("f9dash");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", ctx.Token);

        var summary = await (await _client.GetAsync("/api/v1/dashboard/summary")).Content.ReadFromJsonAsync<JsonElement>();
        summary.GetProperty("success").GetBoolean().Should().BeTrue();
        summary.GetProperty("data").GetProperty("monthSales").GetDecimal().Should().BeGreaterThanOrEqualTo(0);

        var trend = await (await _client.GetAsync("/api/v1/dashboard/sales-trend?period=0")).Content.ReadFromJsonAsync<JsonElement>();
        trend.GetProperty("data").GetArrayLength().Should().BeGreaterThan(0);

        var revenueExpense = await (await _client.GetAsync("/api/v1/dashboard/revenue-expense")).Content.ReadFromJsonAsync<JsonElement>();
        revenueExpense.GetProperty("data").GetArrayLength().Should().Be(6);

        var pending = await (await _client.GetAsync("/api/v1/dashboard/pending-orders")).Content.ReadFromJsonAsync<JsonElement>();
        pending.GetProperty("success").GetBoolean().Should().BeTrue();
    }

    [Fact]
    public async Task Sales_report_and_csv_export_should_work()
    {
        var ctx = await SetupAsync("f9rpt");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", ctx.Token);

        await CreateConfirmedSaleAsync(ctx);

        var from = DateTime.UtcNow.AddDays(-7).ToString("yyyy-MM-dd");
        var to = DateTime.UtcNow.AddDays(1).ToString("yyyy-MM-dd");

        var salesReport = await (await _client.GetAsync($"/api/v1/reports/sales?from={from}&to={to}"))
            .Content.ReadFromJsonAsync<JsonElement>();
        salesReport.GetProperty("data").GetProperty("orderCount").GetInt32().Should().BeGreaterThan(0);
        salesReport.GetProperty("data").GetProperty("totalSales").GetDecimal().Should().BeGreaterThan(0);

        var profitLoss = await (await _client.GetAsync($"/api/v1/reports/profit-loss?from={from}&to={to}"))
            .Content.ReadFromJsonAsync<JsonElement>();
        profitLoss.GetProperty("data").GetProperty("revenue").GetDecimal().Should().BeGreaterThan(0);

        var exportResponse = await _client.GetAsync($"/api/v1/reports/sales/export?format=csv&from={from}&to={to}");
        exportResponse.EnsureSuccessStatusCode();
        exportResponse.Content.Headers.ContentType!.MediaType.Should().Contain("csv");
        (await exportResponse.Content.ReadAsByteArrayAsync()).Length.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task Stock_report_should_work()
    {
        var ctx = await SetupAsync("f9stk");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", ctx.Token);

        var stockReport = await (await _client.GetAsync("/api/v1/reports/stock")).Content.ReadFromJsonAsync<JsonElement>();
        stockReport.GetProperty("data").GetProperty("items").GetArrayLength().Should().BeGreaterThan(0);
    }

    private async Task CreateConfirmedSaleAsync(TestContext ctx)
    {
        await _client.PostAsJsonAsync("/api/v1/stock/adjust", new
        {
            warehouseId = ctx.WarehouseId,
            productVariantId = ctx.VariantId,
            quantityDelta = 50m,
            note = "dashboard test stok"
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
    }

    private async Task<TestContext> SetupAsync(string prefix)
    {
        var slug = $"{prefix}-{Guid.NewGuid():N}"[..18];
        var registerResponse = await _client.PostAsJsonAsync("/api/v1/auth/register", new
        {
            companyName = "Faz9 Dashboard Co",
            slug,
            email = $"{slug}@test.com",
            password = "Password123",
            fullName = "Faz9 Tester"
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
            fullName = "Faz9 Müşteri",
            phone = "05326666666",
            email = $"{prefix}@cust.com",
            creditLimit = 0m
        })).Content.ReadFromJsonAsync<JsonElement>()).GetProperty("data").GetProperty("id").GetGuid();

        return new TestContext(token, branchId, variantId, warehouseId, customerId);
    }

    private record TestContext(string Token, Guid BranchId, Guid VariantId, Guid WarehouseId, Guid CustomerId);
}
