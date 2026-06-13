using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Karasu.ERP.IntegrationTests.Infrastructure;
using Xunit;

namespace Karasu.ERP.IntegrationTests.Finance;

public class FinanceFaz8Tests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public FinanceFaz8Tests(CustomWebApplicationFactory factory) => _client = factory.CreateClient();

    [Fact]
    public async Task Finance_cash_bank_payment_and_summary_flow_should_work()
    {
        var ctx = await SetupAsync("f8fin");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", ctx.Token);

        var cashResponse = await _client.PostAsJsonAsync("/api/v1/finance/cash-registers", new
        {
            branchId = ctx.BranchId,
            name = "Ana Kasa",
            openingBalance = 1000m
        });
        cashResponse.EnsureSuccessStatusCode();
        var cashRegisterId = (await cashResponse.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("data").GetProperty("id").GetGuid();

        var bankResponse = await _client.PostAsJsonAsync("/api/v1/finance/bank-accounts", new
        {
            bankName = "Ziraat",
            accountName = "İşletme Hesabı",
            iban = "TR000000000000000000000001",
            openingBalance = 5000m
        });
        bankResponse.EnsureSuccessStatusCode();

        var expenseResponse = await _client.PostAsJsonAsync("/api/v1/finance/expenses", new
        {
            amount = 200m,
            description = "Kira gideri",
            expenseDate = DateTime.UtcNow,
            paymentMethod = 0,
            cashRegisterId
        });
        expenseResponse.EnsureSuccessStatusCode();

        var incomeResponse = await _client.PostAsJsonAsync("/api/v1/finance/incomes", new
        {
            amount = 500m,
            description = "Nakit satış geliri",
            incomeDate = DateTime.UtcNow,
            source = "Perakende",
            cashRegisterId
        });
        incomeResponse.EnsureSuccessStatusCode();

        var summary = await (await _client.GetAsync("/api/v1/finance/summary")).Content.ReadFromJsonAsync<JsonElement>();
        summary.GetProperty("data").GetProperty("totalCashBalance").GetDecimal().Should().Be(1300m);
        summary.GetProperty("data").GetProperty("totalBankBalance").GetDecimal().Should().Be(5000m);
        summary.GetProperty("data").GetProperty("monthExpense").GetDecimal().Should().Be(200m);
        summary.GetProperty("data").GetProperty("monthIncome").GetDecimal().Should().Be(500m);

        var cashRegisters = await (await _client.GetAsync("/api/v1/finance/cash-registers")).Content.ReadFromJsonAsync<JsonElement>();
        cashRegisters.GetProperty("data").GetArrayLength().Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task Invoice_should_create_receivable_and_payment_should_collect()
    {
        var ctx = await SetupAsync("f8rcv");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", ctx.Token);

        await _client.PostAsJsonAsync("/api/v1/stock/adjust", new
        {
            warehouseId = ctx.WarehouseId,
            productVariantId = ctx.VariantId,
            quantityDelta = 20m,
            note = "finance receivable test"
        }).ContinueWith(r => r.Result.EnsureSuccessStatusCode());

        var cashResponse = await _client.PostAsJsonAsync("/api/v1/finance/cash-registers", new
        {
            branchId = ctx.BranchId,
            name = "Tahsilat Kasası",
            openingBalance = 0m
        });
        cashResponse.EnsureSuccessStatusCode();
        var cashRegisterId = (await cashResponse.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("data").GetProperty("id").GetGuid();

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

        await _client.PostAsync($"/api/v1/orders/{orderId}/confirm", null).ContinueWith(r => r.Result.EnsureSuccessStatusCode());

        var invoiceResponse = await _client.PostAsJsonAsync($"/api/v1/orders/{orderId}/invoice", new
        {
            type = 0,
            issueImmediately = true
        });
        invoiceResponse.EnsureSuccessStatusCode();

        var receivables = await (await _client.GetAsync($"/api/v1/finance/receivables?customerId={ctx.CustomerId}"))
            .Content.ReadFromJsonAsync<JsonElement>();
        receivables.GetProperty("data").GetArrayLength().Should().Be(1);
        var receivableId = receivables.GetProperty("data")[0].GetProperty("id").GetGuid();
        receivables.GetProperty("data")[0].GetProperty("amount").GetDecimal().Should().Be(240m);

        var paymentResponse = await _client.PostAsJsonAsync("/api/v1/finance/payments", new
        {
            direction = 0,
            amount = 240m,
            paidAt = DateTime.UtcNow,
            receivableId,
            cashRegisterId
        });
        paymentResponse.EnsureSuccessStatusCode();

        var payments = await (await _client.GetAsync($"/api/v1/customers/{ctx.CustomerId}/payments")).Content.ReadFromJsonAsync<JsonElement>();
        payments.GetProperty("data").GetArrayLength().Should().Be(1);

        var customer = await (await _client.GetAsync($"/api/v1/customers/{ctx.CustomerId}")).Content.ReadFromJsonAsync<JsonElement>();
        customer.GetProperty("data").GetProperty("balance").GetDecimal().Should().Be(0m);
    }

    [Fact]
    public async Task Customer_note_and_category_brand_create_should_work()
    {
        var ctx = await SetupAsync("f8gap");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", ctx.Token);

        var noteResponse = await _client.PostAsJsonAsync($"/api/v1/customers/{ctx.CustomerId}/notes", new
        {
            content = "VIP müşteri, ödeme takibi yapılacak"
        });
        noteResponse.EnsureSuccessStatusCode();

        var notes = await (await _client.GetAsync($"/api/v1/customers/{ctx.CustomerId}/notes")).Content.ReadFromJsonAsync<JsonElement>();
        notes.GetProperty("data").GetArrayLength().Should().Be(1);

        var categoryResponse = await _client.PostAsJsonAsync("/api/v1/categories", new
        {
            name = $"Kategori-{Guid.NewGuid():N}"[..12],
            sortOrder = 1
        });
        categoryResponse.EnsureSuccessStatusCode();

        var brandResponse = await _client.PostAsJsonAsync("/api/v1/brands", new
        {
            name = $"Marka-{Guid.NewGuid():N}"[..12]
        });
        brandResponse.EnsureSuccessStatusCode();
    }

    private async Task<TestContext> SetupAsync(string prefix)
    {
        var slug = $"{prefix}-{Guid.NewGuid():N}"[..18];
        var registerResponse = await _client.PostAsJsonAsync("/api/v1/auth/register", new
        {
            companyName = "Faz8 Finance Co",
            slug,
            email = $"{slug}@test.com",
            password = "Password123",
            fullName = "Faz8 Tester"
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
            fullName = "Faz8 Müşteri",
            phone = "05325555555",
            email = $"{prefix}@cust.com",
            creditLimit = 10000m
        })).Content.ReadFromJsonAsync<JsonElement>()).GetProperty("data").GetProperty("id").GetGuid();

        return new TestContext(token, branchId, variantId, warehouseId, customerId);
    }

    private record TestContext(string Token, Guid BranchId, Guid VariantId, Guid WarehouseId, Guid CustomerId);
}
