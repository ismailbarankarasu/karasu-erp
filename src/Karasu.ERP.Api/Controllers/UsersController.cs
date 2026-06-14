using Karasu.ERP.Api.Authorization;
using Karasu.ERP.Api.Configuration;
using Karasu.ERP.Application.Features.Users.Commands.CreateUser;
using Karasu.ERP.Application.Features.Users.Commands.DeactivateUser;
using Karasu.ERP.Application.Features.Users.Commands.UpdateUser;
using Karasu.ERP.Application.Features.Users.Queries.GetUserById;
using Karasu.ERP.Application.Features.Users.Queries.GetUsers;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Karasu.ERP.Api.Controllers;

[ApiController]
[Authorize]
[EnableRateLimiting(RateLimitingConfiguration.ApiPolicy)]
[Route("api/v1")]
public class UsersController : ControllerBase
{
    private readonly IMediator _mediator;

    public UsersController(IMediator mediator) => _mediator = mediator;

    [HttpGet("users")]
    [Authorize(Policy = Policies.UserView)]
    public async Task<IActionResult> GetUsers(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? search = null,
        CancellationToken ct = default)
    {
        var result = await _mediator.Send(new GetUsersQuery(page, pageSize, search), ct);
        return result.IsSuccess ? Ok(Wrap(result.Data)) : BadRequest(WrapError(result.Error!, result.ErrorCode));
    }

    [HttpGet("users/{id:guid}")]
    [Authorize(Policy = Policies.UserView)]
    public async Task<IActionResult> GetUser(Guid id, CancellationToken ct)
    {
        var result = await _mediator.Send(new GetUserByIdQuery(id), ct);
        if (!result.IsSuccess) return NotFound(WrapError(result.Error!, result.ErrorCode));
        return Ok(Wrap(result.Data));
    }

    [HttpPost("users")]
    [Authorize(Policy = Policies.UserCreate)]
    public async Task<IActionResult> CreateUser([FromBody] CreateUserCommand command, CancellationToken ct)
    {
        var result = await _mediator.Send(command, ct);
        return result.IsSuccess
            ? CreatedAtAction(nameof(GetUser), new { id = result.Data }, Wrap(new { id = result.Data }))
            : BadRequest(WrapError(result.Error!, result.ErrorCode));
    }

    [HttpPut("users/{id:guid}")]
    [Authorize(Policy = Policies.UserUpdate)]
    public async Task<IActionResult> UpdateUser(Guid id, [FromBody] UpdateUserRequest request, CancellationToken ct)
    {
        var result = await _mediator.Send(new UpdateUserCommand(
            id,
            request.FullName,
            request.IsActive,
            request.RoleIds), ct);

        return result.IsSuccess
            ? Ok(Wrap(new { message = "Kullanıcı güncellendi." }))
            : BadRequest(WrapError(result.Error!, result.ErrorCode));
    }

    [HttpDelete("users/{id:guid}")]
    [Authorize(Policy = Policies.UserDelete)]
    public async Task<IActionResult> DeactivateUser(Guid id, CancellationToken ct)
    {
        var result = await _mediator.Send(new DeactivateUserCommand(id), ct);
        if (!result.IsSuccess) return BadRequest(WrapError(result.Error!, result.ErrorCode));
        return Ok(Wrap(new { message = "Kullanıcı deaktif edildi." }));
    }

    private static object Wrap<T>(T data) => new { success = true, data, errors = (object?)null };

    private static object WrapError(string message, string? code) =>
        new { success = false, data = (object?)null, errors = new[] { new { code = code ?? "ERROR", message } } };
}

public record UpdateUserRequest(string FullName, bool IsActive, List<Guid> RoleIds);
