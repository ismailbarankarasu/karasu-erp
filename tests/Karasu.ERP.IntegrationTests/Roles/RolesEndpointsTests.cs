using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Karasu.ERP.IntegrationTests.Infrastructure;
using Xunit;

namespace Karasu.ERP.IntegrationTests.Roles;

public class RolesEndpointsTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public RolesEndpointsTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Get_roles_should_require_authentication()
    {
        var response = await _client.GetAsync("/api/v1/roles");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Registered_user_should_access_roles_and_permissions()
    {
        var slug = $"roles-{Guid.NewGuid():N}"[..20];
        var registerResponse = await _client.PostAsJsonAsync("/api/v1/auth/register", new
        {
            companyName = "Roles Test Co",
            slug,
            email = $"{slug}@test.com",
            password = "Password123",
            fullName = "Roles Tester"
        });

        registerResponse.EnsureSuccessStatusCode();
        var registerJson = await registerResponse.Content.ReadFromJsonAsync<JsonElement>();
        var token = registerJson.GetProperty("data").GetProperty("accessToken").GetString()!;

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var rolesResponse = await _client.GetAsync("/api/v1/roles");
        rolesResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var permissionsResponse = await _client.GetAsync("/api/v1/permissions");
        permissionsResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var permissionsJson = await permissionsResponse.Content.ReadFromJsonAsync<JsonElement>();
        permissionsJson.GetProperty("data").GetArrayLength().Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task Company_owner_should_create_custom_role()
    {
        var slug = $"create-role-{Guid.NewGuid():N}"[..20];
        var registerResponse = await _client.PostAsJsonAsync("/api/v1/auth/register", new
        {
            companyName = "Create Role Co",
            slug,
            email = $"{slug}@test.com",
            password = "Password123",
            fullName = "Role Creator"
        });

        var registerJson = await registerResponse.Content.ReadFromJsonAsync<JsonElement>();
        var token = registerJson.GetProperty("data").GetProperty("accessToken").GetString()!;
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var createResponse = await _client.PostAsJsonAsync("/api/v1/roles", new
        {
            name = "Accountant",
            description = "Muhasebe departmanı",
            permissionIds = new List<Guid>()
        });

        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
    }
}
