using System.Text;
using System.Text.Json;

namespace Karasu.ERP.IntegrationTests.Infrastructure;

public static class JwtTestHelper
{
    public static Guid GetTenantId(string jwt)
    {
        var parts = jwt.Split('.');
        if (parts.Length < 2)
            throw new InvalidOperationException("Geçersiz JWT.");

        var payload = parts[1];
        payload = payload.PadRight(payload.Length + (4 - payload.Length % 4) % 4, '=');
        var json = Encoding.UTF8.GetString(Convert.FromBase64String(payload.Replace('-', '+').Replace('_', '/')));
        using var doc = JsonDocument.Parse(json);
        return doc.RootElement.GetProperty("tenant_id").GetGuid();
    }
}
