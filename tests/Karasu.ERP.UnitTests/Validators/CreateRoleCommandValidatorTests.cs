using FluentAssertions;
using Karasu.ERP.Application.Features.Roles.Commands.CreateRole;
using Xunit;

namespace Karasu.ERP.UnitTests.Validators;

public class CreateRoleCommandValidatorTests
{
    private readonly CreateRoleCommandValidator _validator = new();

    [Fact]
    public void Should_fail_when_name_empty()
    {
        var result = _validator.Validate(new CreateRoleCommand("", null, null));
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Should_pass_with_valid_name()
    {
        var result = _validator.Validate(new CreateRoleCommand("Accountant", "Muhasebe rolü", null));
        result.IsValid.Should().BeTrue();
    }
}
