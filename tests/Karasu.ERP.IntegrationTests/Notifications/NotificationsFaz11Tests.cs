using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Karasu.ERP.Application.Common.Interfaces;
using Karasu.ERP.Domain.Enums;
using Karasu.ERP.IntegrationTests.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Karasu.ERP.IntegrationTests.Notifications;

public class NotificationsFaz11Tests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public NotificationsFaz11Tests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Notifications_list_and_mark_read_should_work()
    {
        var token = await RegisterAndGetTokenAsync("f11notif");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        using (var scope = _factory.Services.CreateScope())
        {
            var tenantContext = scope.ServiceProvider.GetRequiredService<ITenantContext>();
            tenantContext.TenantId = JwtTestHelper.GetTenantId(token);
            var notificationService = scope.ServiceProvider.GetRequiredService<INotificationService>();

            await notificationService.CreateAsync(
                tenantContext.TenantId,
                new NotificationCreateRequest(
                    null,
                    NotificationType.SystemAnnouncement,
                    "Test Duyuru",
                    "Sprint 11 bildirim testi"),
                CancellationToken.None);
        }

        var list = await (await _client.GetAsync("/api/v1/notifications")).Content.ReadFromJsonAsync<JsonElement>();
        list.GetProperty("data").GetProperty("items").GetArrayLength().Should().BeGreaterThan(0);

        var notificationId = list.GetProperty("data").GetProperty("items")[0].GetProperty("id").GetGuid();

        var markRead = await _client.PatchAsync($"/api/v1/notifications/{notificationId}/read", null);
        markRead.EnsureSuccessStatusCode();

        var markAll = await _client.PatchAsync("/api/v1/notifications/read-all", null);
        markAll.EnsureSuccessStatusCode();
    }

    private async Task<string> RegisterAndGetTokenAsync(string prefix)
    {
        var slug = $"{prefix}-{Guid.NewGuid():N}"[..18];
        var registerResponse = await _client.PostAsJsonAsync("/api/v1/auth/register", new
        {
            companyName = "Faz11 Notification Co",
            slug,
            email = $"{slug}@test.com",
            password = "Password123",
            fullName = "Faz11 Tester"
        });
        registerResponse.EnsureSuccessStatusCode();

        return (await registerResponse.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("data").GetProperty("accessToken").GetString()!;
    }
}
