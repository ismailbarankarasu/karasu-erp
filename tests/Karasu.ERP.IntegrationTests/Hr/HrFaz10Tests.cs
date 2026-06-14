using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Karasu.ERP.IntegrationTests.Infrastructure;
using Xunit;

namespace Karasu.ERP.IntegrationTests.Hr;

public class HrFaz10Tests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public HrFaz10Tests(CustomWebApplicationFactory factory) => _client = factory.CreateClient();

    [Fact]
    public async Task Employee_leave_shift_and_payroll_should_work()
    {
        var ctx = await SetupAsync("f10hr");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", ctx.Token);

        var employeeResponse = await _client.PostAsJsonAsync("/api/v1/hr/employees", new
        {
            employeeNo = "EMP-001",
            fullName = "Test Personel",
            department = "Satış",
            position = "Uzman",
            hireDate = DateTime.UtcNow.AddYears(-1).ToString("yyyy-MM-dd"),
            salary = 25000m
        });
        employeeResponse.EnsureSuccessStatusCode();
        var employeeId = (await employeeResponse.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("data").GetProperty("id").GetGuid();

        var leaveResponse = await _client.PostAsJsonAsync("/api/v1/hr/leave-requests", new
        {
            employeeId,
            type = 0,
            startDate = DateTime.UtcNow.AddDays(7).ToString("yyyy-MM-dd"),
            endDate = DateTime.UtcNow.AddDays(10).ToString("yyyy-MM-dd"),
            reason = "Yıllık izin"
        });
        leaveResponse.EnsureSuccessStatusCode();
        var leaveId = (await leaveResponse.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("data").GetProperty("id").GetGuid();

        var approveResponse = await _client.PatchAsJsonAsync($"/api/v1/hr/leave-requests/{leaveId}/approve", new { approve = true });
        approveResponse.EnsureSuccessStatusCode();

        var shiftResponse = await _client.PostAsJsonAsync("/api/v1/hr/shifts", new
        {
            employeeId,
            branchId = ctx.BranchId,
            date = DateTime.UtcNow.ToString("yyyy-MM-dd"),
            startTime = "09:00:00",
            endTime = "18:00:00"
        });
        shiftResponse.EnsureSuccessStatusCode();

        var period = DateTime.UtcNow.ToString("yyyy-MM");
        var payrollResponse = await _client.PostAsJsonAsync("/api/v1/hr/payrolls/generate", new { period });
        payrollResponse.EnsureSuccessStatusCode();
        var payrollResult = await payrollResponse.Content.ReadFromJsonAsync<JsonElement>();
        payrollResult.GetProperty("data").GetProperty("generatedCount").GetInt32().Should().BeGreaterThan(0);

        var employees = await (await _client.GetAsync("/api/v1/hr/employees")).Content.ReadFromJsonAsync<JsonElement>();
        employees.GetProperty("data").GetProperty("items").GetArrayLength().Should().BeGreaterThan(0);
    }

    private async Task<TestContext> SetupAsync(string prefix)
    {
        var slug = $"{prefix}-{Guid.NewGuid():N}"[..18];
        var registerResponse = await _client.PostAsJsonAsync("/api/v1/auth/register", new
        {
            companyName = "Faz10 HR Co",
            slug,
            email = $"{slug}@test.com",
            password = "Password123",
            fullName = "Faz10 Tester"
        });
        registerResponse.EnsureSuccessStatusCode();

        var token = (await registerResponse.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("data").GetProperty("accessToken").GetString()!;

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var branchId = (await (await _client.GetAsync("/api/v1/branches")).Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("data")[0].GetProperty("id").GetGuid();

        return new TestContext(token, branchId);
    }

    private record TestContext(string Token, Guid BranchId);
}
