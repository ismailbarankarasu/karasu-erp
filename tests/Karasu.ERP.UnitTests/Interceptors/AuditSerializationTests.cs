using FluentAssertions;
using System.Text.Json;
using Xunit;

namespace Karasu.ERP.UnitTests.Interceptors;

public class AuditSerializationTests
{
    [Fact]
    public void JsonSerializer_should_handle_scalar_dictionary()
    {
        var values = new Dictionary<string, object?>
        {
            ["Name"] = "Test Product",
            ["Price"] = 99.99m,
            ["IsActive"] = true
        };

        var json = JsonSerializer.Serialize(values);
        json.Should().Contain("Test Product");
        json.Should().Contain("99.99");
    }
}
