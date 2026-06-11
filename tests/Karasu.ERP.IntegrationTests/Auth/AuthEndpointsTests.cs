using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Karasu.ERP.IntegrationTests.Infrastructure;
using Xunit;

namespace Karasu.ERP.IntegrationTests.Auth;

public class AuthEndpointsTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public AuthEndpointsTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Register_should_create_tenant_and_return_tokens()
    {
        var slug = $"test-{Guid.NewGuid():N}"[..20];
        var response = await _client.PostAsJsonAsync("/api/v1/auth/register", new
        {
            companyName = "Test Company",
            slug,
            email = $"{slug}@test.com",
            password = "Password123",
            fullName = "Test Owner"
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        json.GetProperty("success").GetBoolean().Should().BeTrue();
        json.GetProperty("data").GetProperty("accessToken").GetString().Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task Login_should_fail_with_invalid_credentials()
    {
        var response = await _client.PostAsJsonAsync("/api/v1/auth/login", new
        {
            email = "nonexistent@test.com",
            password = "WrongPass123"
        });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Health_endpoint_should_return_ok()
    {
        var response = await _client.GetAsync("/api/v1/health");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
