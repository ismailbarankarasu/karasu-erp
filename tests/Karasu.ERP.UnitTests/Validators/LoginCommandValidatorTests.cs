using FluentAssertions;
using Karasu.ERP.Application.Features.Auth.Commands.Login;
using Xunit;

namespace Karasu.ERP.UnitTests.Validators;

public class LoginCommandValidatorTests
{
    private readonly LoginCommandValidator _validator = new();

    [Fact]
    public void Should_fail_when_email_is_empty()
    {
        var result = _validator.Validate(new LoginCommand("", "Password123"));
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(LoginCommand.Email));
    }

    [Fact]
    public void Should_fail_when_password_is_too_short()
    {
        var result = _validator.Validate(new LoginCommand("user@test.com", "short"));
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(LoginCommand.Password));
    }

    [Fact]
    public void Should_pass_with_valid_input()
    {
        var result = _validator.Validate(new LoginCommand("user@test.com", "Password123"));
        result.IsValid.Should().BeTrue();
    }
}
