using FluentAssertions;
using Karasu.ERP.Application.Features.Auth.Commands.Register;
using Xunit;

namespace Karasu.ERP.UnitTests.Validators;

public class RegisterCommandValidatorTests
{
    private readonly RegisterCommandValidator _validator = new();

    [Fact]
    public void Should_fail_when_slug_has_invalid_characters()
    {
        var result = _validator.Validate(new RegisterCommand(
            "Test Co", "Invalid Slug!", "a@b.com", "Password123", "Test User"));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(RegisterCommand.Slug));
    }

    [Fact]
    public void Should_pass_with_valid_input()
    {
        var result = _validator.Validate(new RegisterCommand(
            "Test Company", "test-company", "owner@test.com", "Password123", "Test Owner"));

        result.IsValid.Should().BeTrue();
    }
}
