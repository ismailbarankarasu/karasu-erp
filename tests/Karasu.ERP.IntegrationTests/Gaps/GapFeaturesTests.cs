using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using Karasu.ERP.IntegrationTests.Infrastructure;
using Xunit;

namespace Karasu.ERP.IntegrationTests.Gaps;

public class GapFeaturesTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public GapFeaturesTests(CustomWebApplicationFactory factory) => _client = factory.CreateClient();

    [Fact]
    public async Task Branch_crud_should_work_for_registered_owner()
    {
        await AuthenticateNewTenantAsync("branch");

        var createResponse = await _client.PostAsJsonAsync("/api/v1/branches", new
        {
            name = "Yan Şube",
            code = "SUB01",
            address = "Test Cad.",
            city = "İstanbul",
            phone = "5550000000"
        });

        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var createJson = await createResponse.Content.ReadFromJsonAsync<JsonElement>();
        var branchId = createJson.GetProperty("data").GetProperty("id").GetGuid();

        var listResponse = await _client.GetAsync("/api/v1/branches");
        listResponse.EnsureSuccessStatusCode();
        var listJson = await listResponse.Content.ReadFromJsonAsync<JsonElement>();
        listJson.GetProperty("data").GetArrayLength().Should().BeGreaterThanOrEqualTo(2);

        var updateResponse = await _client.PutAsJsonAsync($"/api/v1/branches/{branchId}", new
        {
            name = "Güncel Şube",
            code = "SUB01",
            address = "Yeni Cad.",
            city = "Ankara",
            phone = "5551111111",
            isActive = true
        });
        updateResponse.EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task Product_export_and_barcode_generate_should_work()
    {
        await AuthenticateNewTenantAsync("barcode");
        var unitId = await GetDefaultUnitIdAsync();

        var createResponse = await _client.PostAsJsonAsync("/api/v1/products", new
        {
            sku = "EXP-001",
            barcode = (string?)null,
            name = "Export Ürün",
            categoryId = (Guid?)null,
            brandId = (Guid?)null,
            unitId,
            purchasePrice = 10m,
            salePrice = 20m,
            taxRate = 20m,
            minStock = 1m
        });
        createResponse.EnsureSuccessStatusCode();
        var productId = (await createResponse.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("data").GetProperty("id").GetGuid();

        var barcodeResponse = await _client.PostAsync($"/api/v1/products/{productId}/barcode/generate", null);
        barcodeResponse.EnsureSuccessStatusCode();
        var barcodeJson = await barcodeResponse.Content.ReadFromJsonAsync<JsonElement>();
        barcodeJson.GetProperty("data").GetProperty("barcode").GetString().Should().NotBeNullOrEmpty();

        var exportResponse = await _client.GetAsync("/api/v1/products/export");
        exportResponse.EnsureSuccessStatusCode();
        exportResponse.Content.Headers.ContentType!.MediaType.Should()
            .Be("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");
    }

    [Fact]
    public async Task Customer_attachment_upload_should_work()
    {
        await AuthenticateNewTenantAsync("attach");

        var createCustomer = await _client.PostAsJsonAsync("/api/v1/customers", new
        {
            type = 0,
            fullName = "Ek Dosya Müşteri",
            companyName = (string?)null,
            taxNumber = (string?)null,
            phone = "5552222222",
            email = "attach@test.com",
            address = "Adres",
            city = "İzmir",
            creditLimit = 1000m
        });
        createCustomer.EnsureSuccessStatusCode();
        var customerId = (await createCustomer.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("data").GetProperty("id").GetGuid();

        using var content = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(Encoding.UTF8.GetBytes("test attachment"));
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("text/plain");
        content.Add(fileContent, "file", "not.txt");

        var uploadResponse = await _client.PostAsync($"/api/v1/customers/{customerId}/attachments", content);
        uploadResponse.EnsureSuccessStatusCode();

        var listResponse = await _client.GetAsync($"/api/v1/customers/{customerId}/attachments");
        listResponse.EnsureSuccessStatusCode();
        var listJson = await listResponse.Content.ReadFromJsonAsync<JsonElement>();
        listJson.GetProperty("data").GetArrayLength().Should().Be(1);
    }

    private async Task AuthenticateNewTenantAsync(string prefix)
    {
        var slug = $"{prefix}-{Guid.NewGuid():N}"[..18];
        var registerResponse = await _client.PostAsJsonAsync("/api/v1/auth/register", new
        {
            companyName = $"{prefix} Test Co",
            slug,
            email = $"{slug}@test.com",
            password = "Password123",
            fullName = "Gap Tester"
        });

        registerResponse.EnsureSuccessStatusCode();
        var registerJson = await registerResponse.Content.ReadFromJsonAsync<JsonElement>();
        var token = registerJson.GetProperty("data").GetProperty("accessToken").GetString()!;
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
    }

    private async Task<Guid> GetDefaultUnitIdAsync()
    {
        var unitsResponse = await _client.GetAsync("/api/v1/units");
        unitsResponse.EnsureSuccessStatusCode();
        var unitsJson = await unitsResponse.Content.ReadFromJsonAsync<JsonElement>();
        return unitsJson.GetProperty("data")[0].GetProperty("id").GetGuid();
    }
}
