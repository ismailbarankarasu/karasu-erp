using Karasu.ERP.Application.Features.Auth.Commands.ChangePassword;
using Karasu.ERP.Application.Features.Auth.Commands.ForgotPassword;
using Karasu.ERP.Application.Features.Auth.Commands.Login;
using Karasu.ERP.Application.Features.Auth.Commands.Logout;
using Karasu.ERP.Application.Features.Auth.Commands.RefreshToken;
using Karasu.ERP.Application.Features.Auth.Commands.Register;
using Karasu.ERP.Application.Features.Auth.Commands.ResetPassword;
using Karasu.ERP.Application.Features.Auth.Commands.UpdateProfile;
using Karasu.ERP.Application.Features.Auth.Queries.GetCurrentUser;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Karasu.ERP.Api.Configuration;

namespace Karasu.ERP.Api.Controllers;

[ApiController]
[Route("api/v1/auth")]
[EnableRateLimiting(RateLimitingConfiguration.AuthPolicy)]
public class AuthController : ControllerBase
{
    private readonly IMediator _mediator;

    public AuthController(IMediator mediator) => _mediator = mediator;

    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<IActionResult> Register([FromBody] RegisterCommand command, CancellationToken ct)
    {
        var result = await _mediator.Send(command, ct);
        return result.IsSuccess ? Ok(Wrap(result.Data)) : BadRequest(WrapError(result.Error!, result.ErrorCode));
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login([FromBody] LoginCommand command, CancellationToken ct)
    {
        var result = await _mediator.Send(command, ct);
        return result.IsSuccess ? Ok(Wrap(result.Data)) : Unauthorized(WrapError(result.Error!, result.ErrorCode));
    }

    [HttpPost("refresh")]
    [AllowAnonymous]
    public async Task<IActionResult> Refresh([FromBody] RefreshTokenCommand command, CancellationToken ct)
    {
        var result = await _mediator.Send(command, ct);
        return result.IsSuccess ? Ok(Wrap(result.Data)) : Unauthorized(WrapError(result.Error!, result.ErrorCode));
    }

    [HttpPost("logout")]
    [Authorize]
    public async Task<IActionResult> Logout([FromBody] LogoutCommand command, CancellationToken ct)
    {
        var result = await _mediator.Send(command, ct);
        return result.IsSuccess ? Ok(Wrap(new { message = "Çıkış yapıldı." })) : BadRequest(WrapError(result.Error!, result.ErrorCode));
    }

    [HttpGet("me")]
    [Authorize]
    public async Task<IActionResult> Me(CancellationToken ct)
    {
        var result = await _mediator.Send(new GetCurrentUserQuery(), ct);
        return result.IsSuccess ? Ok(Wrap(result.Data)) : Unauthorized(WrapError(result.Error!, result.ErrorCode));
    }

    [HttpPut("me")]
    [Authorize]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileRequest request, CancellationToken ct)
    {
        var result = await _mediator.Send(new UpdateProfileCommand(request.FullName, request.Email), ct);
        return result.IsSuccess ? Ok(Wrap(result.Data)) : BadRequest(WrapError(result.Error!, result.ErrorCode));
    }

    [HttpPut("change-password")]
    [Authorize]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request, CancellationToken ct)
    {
        var result = await _mediator.Send(new ChangePasswordCommand(request.CurrentPassword, request.NewPassword), ct);
        return result.IsSuccess ? Ok(Wrap(new { message = "Şifre güncellendi." })) : BadRequest(WrapError(result.Error!, result.ErrorCode));
    }

    [HttpPost("forgot-password")]
    [AllowAnonymous]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request, CancellationToken ct)
    {
        var result = await _mediator.Send(new ForgotPasswordCommand(request.Email), ct);
        return result.IsSuccess ? Ok(Wrap(result.Data)) : BadRequest(WrapError(result.Error!, result.ErrorCode));
    }

    [HttpPost("reset-password")]
    [AllowAnonymous]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordCommand command, CancellationToken ct)
    {
        var result = await _mediator.Send(command, ct);
        return result.IsSuccess ? Ok(Wrap(new { message = "Şifre sıfırlandı." })) : BadRequest(WrapError(result.Error!, result.ErrorCode));
    }

    private static object Wrap<T>(T data) => new { success = true, data, errors = (object?)null };

    private static object WrapError(string message, string? code) =>
        new { success = false, data = (object?)null, errors = new[] { new { code = code ?? "ERROR", message } } };
}

public record UpdateProfileRequest(string FullName, string? Email);
public record ChangePasswordRequest(string CurrentPassword, string NewPassword);
public record ForgotPasswordRequest(string Email);

[ApiController]
[Route("api/v1/health")]
public class HealthController : ControllerBase
{
    [HttpGet]
    [AllowAnonymous]
    public IActionResult Get() =>
        Ok(new
        {
            success = true,
            data = new
            {
                status = "Healthy",
                service = "Karasu ERP API",
                timestamp = DateTime.UtcNow
            }
        });
}
