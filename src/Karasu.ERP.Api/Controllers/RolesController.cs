using Karasu.ERP.Api.Authorization;
using Karasu.ERP.Application.Features.AuditLogs.Queries.GetAuditLogs;
using Karasu.ERP.Application.Features.Permissions.Queries.GetPermissions;
using Karasu.ERP.Application.Features.Roles.Commands.CreateRole;
using Karasu.ERP.Application.Features.Roles.Commands.UpdateRole;
using Karasu.ERP.Application.Features.Roles.Commands.UpdateRolePermissions;
using Karasu.ERP.Application.Features.Roles.Queries.GetRoleById;
using Karasu.ERP.Application.Features.Roles.Queries.GetRoles;
using Karasu.ERP.Api.Configuration;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Karasu.ERP.Api.Controllers;

[ApiController]
[Authorize]
[EnableRateLimiting(RateLimitingConfiguration.ApiPolicy)]
[Route("api/v1")]
public class RolesController : ControllerBase
{
    private readonly IMediator _mediator;

    public RolesController(IMediator mediator) => _mediator = mediator;

    [HttpGet("roles")]
    [Authorize(Policy = Policies.RoleView)]
    public async Task<IActionResult> GetRoles(CancellationToken ct)
    {
        var result = await _mediator.Send(new GetRolesQuery(), ct);
        return result.IsSuccess ? Ok(Wrap(result.Data)) : BadRequest(WrapError(result.Error!, result.ErrorCode));
    }

    [HttpGet("roles/{id:guid}")]
    [Authorize(Policy = Policies.RoleView)]
    public async Task<IActionResult> GetRole(Guid id, CancellationToken ct)
    {
        var result = await _mediator.Send(new GetRoleByIdQuery(id), ct);
        if (!result.IsSuccess) return NotFound(WrapError(result.Error!, result.ErrorCode));
        return Ok(Wrap(result.Data));
    }

    [HttpPost("roles")]
    [Authorize(Policy = Policies.RoleCreate)]
    public async Task<IActionResult> CreateRole([FromBody] CreateRoleCommand command, CancellationToken ct)
    {
        var result = await _mediator.Send(command, ct);
        return result.IsSuccess
            ? CreatedAtAction(nameof(GetRole), new { id = result.Data }, Wrap(result.Data))
            : BadRequest(WrapError(result.Error!, result.ErrorCode));
    }

    [HttpPut("roles/{id:guid}")]
    [Authorize(Policy = Policies.RoleUpdate)]
    public async Task<IActionResult> UpdateRole(Guid id, [FromBody] UpdateRoleRequest request, CancellationToken ct)
    {
        var result = await _mediator.Send(new UpdateRoleCommand(id, request.Name, request.Description), ct);
        return result.IsSuccess ? Ok(Wrap(new { message = "Rol güncellendi." })) : BadRequest(WrapError(result.Error!, result.ErrorCode));
    }

    [HttpPut("roles/{id:guid}/permissions")]
    [Authorize(Policy = Policies.RoleUpdate)]
    public async Task<IActionResult> UpdateRolePermissions(Guid id, [FromBody] UpdateRolePermissionsRequest request, CancellationToken ct)
    {
        var result = await _mediator.Send(new UpdateRolePermissionsCommand(id, request.PermissionIds), ct);
        return result.IsSuccess ? Ok(Wrap(new { message = "Rol izinleri güncellendi." })) : BadRequest(WrapError(result.Error!, result.ErrorCode));
    }

    [HttpGet("permissions")]
    [Authorize(Policy = Policies.RoleView)]
    public async Task<IActionResult> GetPermissions(CancellationToken ct)
    {
        var result = await _mediator.Send(new GetPermissionsQuery(), ct);
        return result.IsSuccess ? Ok(Wrap(result.Data)) : BadRequest(WrapError(result.Error!, result.ErrorCode));
    }

    [HttpGet("audit-logs")]
    [Authorize(Policy = Policies.AuditView)]
    public async Task<IActionResult> GetAuditLogs(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? entityType = null,
        [FromQuery] Guid? entityId = null,
        [FromQuery] Guid? userId = null,
        CancellationToken ct = default)
    {
        var result = await _mediator.Send(new GetAuditLogsQuery(page, pageSize, entityType, entityId, userId), ct);
        return result.IsSuccess ? Ok(Wrap(result.Data)) : BadRequest(WrapError(result.Error!, result.ErrorCode));
    }

    private static object Wrap<T>(T data) => new { success = true, data, errors = (object?)null };

    private static object WrapError(string message, string? code) =>
        new { success = false, data = (object?)null, errors = new[] { new { code = code ?? "ERROR", message } } };
}

public record UpdateRoleRequest(string Name, string? Description);
public record UpdateRolePermissionsRequest(List<Guid> PermissionIds);
