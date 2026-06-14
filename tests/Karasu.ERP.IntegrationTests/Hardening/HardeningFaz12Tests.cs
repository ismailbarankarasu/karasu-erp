using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using Karasu.ERP.IntegrationTests.Infrastructure;
using Xunit;

namespace Karasu.ERP.IntegrationTests.Hardening;

public class HardeningFaz12Tests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public HardeningFaz12Tests(CustomWebApplicationFactory factory) => _client = factory.CreateClient();

    [Fact]
    public async Task Security_headers_should_be_present()
    {
        var response = await _client.GetAsync("/health/live");
        response.EnsureSuccessStatusCode();

        response.Headers.TryGetValues("X-Content-Type-Options", out var contentTypeOptions).Should().BeTrue();
        contentTypeOptions!.First().Should().Be("nosniff");

        response.Headers.TryGetValues("X-Frame-Options", out var frameOptions).Should().BeTrue();
        frameOptions!.First().Should().Be("DENY");

        response.Headers.TryGetValues("Referrer-Policy", out var referrerPolicy).Should().BeTrue();
        referrerPolicy!.First().Should().Be("strict-origin-when-cross-origin");
    }

    [Fact]
    public async Task Openapi_document_should_be_available_in_development()
    {
        var response = await _client.GetAsync("/openapi/v1/openapi.json");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var json = await response.Content.ReadAsStringAsync();
        json.Should().Contain("Karasu ERP API");
    }

    [Fact]
    public async Task Auth_endpoints_should_return_429_when_rate_limited()
    {
        HttpResponseMessage? lastResponse = null;
        for (var i = 0; i < 210; i++)
        {
            lastResponse = await _client.PostAsJsonAsync("/api/v1/auth/login", new
            {
                email = "nonexistent@test.com",
                password = "WrongPassword123"
            });
        }

        lastResponse.Should().NotBeNull();
        lastResponse!.StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
    }
}
