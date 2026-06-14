using Karasu.ERP.Api.Authorization;
using Karasu.ERP.Api.Configuration;
using Karasu.ERP.Application.Features.Tenants.Commands.UpdateCurrentTenant;
using Karasu.ERP.Application.Features.Tenants.Commands.UpdateTenantSettings;
using Karasu.ERP.Application.Features.Tenants.Queries.GetCurrentTenant;
using Karasu.ERP.Application.Features.Tenants.Queries.GetTenantSettings;
using Karasu.ERP.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System.Text.Json;

namespace Karasu.ERP.Api.Controllers;

[ApiController]
[Authorize]
[EnableRateLimiting(RateLimitingConfiguration.ApiPolicy)]
[Route("api/v1")]
public class TenantsController : ControllerBase
{
    private readonly IMediator _mediator;

    public TenantsController(IMediator mediator) => _mediator = mediator;

    [HttpGet("tenants/current")]
    public async Task<IActionResult> GetCurrent(CancellationToken ct)
    {
        var result = await _mediator.Send(new GetCurrentTenantQuery(), ct);
        return result.IsSuccess ? Ok(Wrap(result.Data)) : NotFound(WrapError(result.Error!, result.ErrorCode));
    }

    [HttpPut("tenants/current")]
    [Authorize(Roles = "CompanyOwner")]
    public async Task<IActionResult> UpdateCurrent([FromBody] UpdateCurrentTenantRequest request, CancellationToken ct)
    {
        var result = await _mediator.Send(new UpdateCurrentTenantCommand(
            request.Name,
            request.BusinessType,
            request.Plan), ct);

        return result.IsSuccess
            ? Ok(Wrap(new { message = "Tenant güncellendi." }))
            : BadRequest(WrapError(result.Error!, result.ErrorCode));
    }

    [HttpGet("tenants/current/settings")]
    public async Task<IActionResult> GetSettings(CancellationToken ct)
    {
        var result = await _mediator.Send(new GetTenantSettingsQuery(), ct);
        return result.IsSuccess ? Ok(Wrap(result.Data)) : NotFound(WrapError(result.Error!, result.ErrorCode));
    }

    [HttpPut("tenants/current/settings")]
    [Authorize(Roles = "CompanyOwner")]
    public async Task<IActionResult> UpdateSettings([FromBody] Dictionary<string, JsonElement> settings, CancellationToken ct)
    {
        var result = await _mediator.Send(new UpdateTenantSettingsCommand(settings), ct);
        return result.IsSuccess
            ? Ok(Wrap(new { message = "Ayarlar güncellendi." }))
            : BadRequest(WrapError(result.Error!, result.ErrorCode));
    }

    private static object Wrap<T>(T data) => new { success = true, data, errors = (object?)null };

    private static object WrapError(string message, string? code) =>
        new { success = false, data = (object?)null, errors = new[] { new { code = code ?? "ERROR", message } } };
}

public record UpdateCurrentTenantRequest(string Name, BusinessType BusinessType, SubscriptionPlan Plan);
