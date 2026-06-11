using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Karasu.ERP.IntegrationTests.Infrastructure;
using Xunit;

namespace Karasu.ERP.IntegrationTests.Customers;

public class CustomersEndpointsTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public CustomersEndpointsTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Get_customers_should_require_authentication()
    {
        var response = await _client.GetAsync("/api/v1/customers");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Registered_user_should_create_update_and_delete_customer()
    {
        var slug = $"cust-{Guid.NewGuid():N}"[..18];
        var registerResponse = await _client.PostAsJsonAsync("/api/v1/auth/register", new
        {
            companyName = "Customer Test Co",
            slug,
            email = $"{slug}@test.com",
            password = "Password123",
            fullName = "Customer Tester"
        });

        registerResponse.EnsureSuccessStatusCode();
        var registerJson = await registerResponse.Content.ReadFromJsonAsync<JsonElement>();
        var token = registerJson.GetProperty("data").GetProperty("accessToken").GetString()!;

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var createResponse = await _client.PostAsJsonAsync("/api/v1/customers", new
        {
            type = 0,
            fullName = "Ahmet Yılmaz",
            companyName = (string?)null,
            taxNumber = (string?)null,
            phone = "05321234567",
            email = "ahmet@test.com",
            address = "Test Mah. No:1",
            city = "İstanbul",
            creditLimit = 5000m
        });

        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var createJson = await createResponse.Content.ReadFromJsonAsync<JsonElement>();
        var customerId = createJson.GetProperty("data").GetProperty("id").GetGuid();

        var listResponse = await _client.GetAsync("/api/v1/customers");
        listResponse.EnsureSuccessStatusCode();
        var listJson = await listResponse.Content.ReadFromJsonAsync<JsonElement>();
        listJson.GetProperty("data").GetProperty("items").GetArrayLength().Should().Be(1);

        var updateResponse = await _client.PutAsJsonAsync($"/api/v1/customers/{customerId}", new
        {
            type = 0,
            fullName = "Ahmet Yılmaz (Güncellendi)",
            companyName = (string?)null,
            taxNumber = (string?)null,
            phone = "05329999999",
            email = "ahmet@test.com",
            address = "Yeni Adres",
            city = "Ankara",
            creditLimit = 10000m,
            status = 0
        });
        updateResponse.EnsureSuccessStatusCode();

        var deleteResponse = await _client.DeleteAsync($"/api/v1/customers/{customerId}");
        deleteResponse.EnsureSuccessStatusCode();

        var listAfterDelete = await _client.GetAsync("/api/v1/customers");
        var listAfterDeleteJson = await listAfterDelete.Content.ReadFromJsonAsync<JsonElement>();
        listAfterDeleteJson.GetProperty("data").GetProperty("items").GetArrayLength().Should().Be(0);
    }
}
