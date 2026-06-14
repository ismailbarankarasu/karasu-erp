using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Karasu.ERP.IntegrationTests.Infrastructure;
using Xunit;

namespace Karasu.ERP.IntegrationTests.Gaps;

public class ApiGapsTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public ApiGapsTests(CustomWebApplicationFactory factory) => _client = factory.CreateClient();

    [Fact]
    public async Task Auth_profile_password_and_reset_flow_should_work()
    {
        var slug = $"auth-{Guid.NewGuid():N}"[..18];
        var email = $"{slug}@test.com";
        const string password = "Password123";

        var registerResponse = await _client.PostAsJsonAsync("/api/v1/auth/register", new
        {
            companyName = "Auth Gap Co",
            slug,
            email,
            password,
            fullName = "Auth Tester"
        });
        registerResponse.EnsureSuccessStatusCode();

        var token = (await registerResponse.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("data").GetProperty("accessToken").GetString()!;
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var updateProfile = await _client.PutAsJsonAsync("/api/v1/auth/me", new
        {
            fullName = "Güncel Ad",
            email
        });
        updateProfile.EnsureSuccessStatusCode();

        var changePassword = await _client.PutAsJsonAsync("/api/v1/auth/change-password", new
        {
            currentPassword = password,
            newPassword = "NewPassword123"
        });
        changePassword.EnsureSuccessStatusCode();

        var forgot = await _client.PostAsJsonAsync("/api/v1/auth/forgot-password", new { email });
        forgot.EnsureSuccessStatusCode();
        var forgotJson = await forgot.Content.ReadFromJsonAsync<JsonElement>();
        var resetToken = forgotJson.GetProperty("data").GetProperty("resetToken").GetString();
        resetToken.Should().NotBeNullOrEmpty();

        _client.DefaultRequestHeaders.Authorization = null;
        var reset = await _client.PostAsJsonAsync("/api/v1/auth/reset-password", new
        {
            email,
            token = resetToken,
            newPassword = "ResetPassword123"
        });
        reset.EnsureSuccessStatusCode();

        var login = await _client.PostAsJsonAsync("/api/v1/auth/login", new
        {
            email,
            password = "ResetPassword123"
        });
        login.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Tenant_users_variants_and_customer_orders_should_work()
    {
        var slug = $"api-{Guid.NewGuid():N}"[..18];
        var registerResponse = await _client.PostAsJsonAsync("/api/v1/auth/register", new
        {
            companyName = "API Gap Co",
            slug,
            email = $"{slug}@test.com",
            password = "Password123",
            fullName = "API Tester"
        });
        registerResponse.EnsureSuccessStatusCode();
        var token = (await registerResponse.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("data").GetProperty("accessToken").GetString()!;
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var tenantResponse = await _client.GetAsync("/api/v1/tenants/current");
        tenantResponse.EnsureSuccessStatusCode();

        var settingsPut = await _client.PutAsJsonAsync("/api/v1/tenants/current/settings", new Dictionary<string, object>
        {
            ["currency"] = "TRY",
            ["locale"] = "tr-TR"
        });
        settingsPut.EnsureSuccessStatusCode();

        var settingsGet = await _client.GetAsync("/api/v1/tenants/current/settings");
        settingsGet.EnsureSuccessStatusCode();

        var usersResponse = await _client.GetAsync("/api/v1/users");
        usersResponse.EnsureSuccessStatusCode();

        var rolesResponse = await _client.GetAsync("/api/v1/roles");
        rolesResponse.EnsureSuccessStatusCode();
        var roleId = (await rolesResponse.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("data")[0].GetProperty("id").GetGuid();

        var createUser = await _client.PostAsJsonAsync("/api/v1/users", new
        {
            email = $"staff-{slug}@test.com",
            password = "Password123",
            fullName = "Staff User",
            roleIds = new[] { roleId }
        });
        createUser.EnsureSuccessStatusCode();

        var unitsResponse = await _client.GetAsync("/api/v1/units");
        unitsResponse.EnsureSuccessStatusCode();
        var unitId = (await unitsResponse.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("data")[0].GetProperty("id").GetGuid();

        var productResponse = await _client.PostAsJsonAsync("/api/v1/products", new
        {
            sku = $"VAR-{slug}",
            barcode = (string?)null,
            name = "Varyantlı Ürün",
            categoryId = (Guid?)null,
            brandId = (Guid?)null,
            unitId,
            purchasePrice = 10m,
            salePrice = 20m,
            taxRate = 20m,
            minStock = 1m
        });
        productResponse.EnsureSuccessStatusCode();
        var productId = (await productResponse.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("data").GetProperty("id").GetGuid();

        var variantResponse = await _client.PostAsJsonAsync($"/api/v1/products/{productId}/variants", new
        {
            sku = $"VAR-{slug}-L",
            barcode = "8690000000999",
            purchasePrice = 12m,
            salePrice = 24m,
            attributesJson = "{\"size\":\"L\"}"
        });
        variantResponse.EnsureSuccessStatusCode();

        var customerResponse = await _client.PostAsJsonAsync("/api/v1/customers", new
        {
            type = 0,
            fullName = "Sipariş Müşteri",
            companyName = (string?)null,
            taxNumber = (string?)null,
            phone = "5553333333",
            email = $"cust-{slug}@test.com",
            address = "Adres",
            city = "Bursa",
            creditLimit = 5000m
        });
        customerResponse.EnsureSuccessStatusCode();
        var customerId = (await customerResponse.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("data").GetProperty("id").GetGuid();

        var ordersResponse = await _client.GetAsync($"/api/v1/customers/{customerId}/orders");
        ordersResponse.EnsureSuccessStatusCode();
        var ordersJson = await ordersResponse.Content.ReadFromJsonAsync<JsonElement>();
        ordersJson.GetProperty("data").GetProperty("items").GetArrayLength().Should().Be(0);
    }
}
