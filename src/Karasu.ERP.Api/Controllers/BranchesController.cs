using Karasu.ERP.Api.Authorization;
using Karasu.ERP.Api.Configuration;
using Karasu.ERP.Application.Features.Branches.Commands.CreateBranch;
using Karasu.ERP.Application.Features.Branches.Commands.DeleteBranch;
using Karasu.ERP.Application.Features.Branches.Commands.UpdateBranch;
using Karasu.ERP.Application.Features.Branches.Queries.GetBranchById;
using Karasu.ERP.Application.Features.Orders.Queries.GetBranches;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Karasu.ERP.Api.Controllers;

[ApiController]
[Authorize]
[EnableRateLimiting(RateLimitingConfiguration.ApiPolicy)]
[Route("api/v1")]
public class BranchesController : ControllerBase
{
    private readonly IMediator _mediator;

    public BranchesController(IMediator mediator) => _mediator = mediator;

    [HttpGet("branches")]
    [Authorize(Policy = Policies.BranchView)]
    public async Task<IActionResult> GetBranches(CancellationToken ct)
    {
        var result = await _mediator.Send(new GetBranchesQuery(), ct);
        return result.IsSuccess ? Ok(Wrap(result.Data)) : BadRequest(WrapError(result.Error!, result.ErrorCode));
    }

    [HttpGet("branches/{id:guid}")]
    [Authorize(Policy = Policies.BranchView)]
    public async Task<IActionResult> GetBranch(Guid id, CancellationToken ct)
    {
        var result = await _mediator.Send(new GetBranchByIdQuery(id), ct);
        if (!result.IsSuccess) return NotFound(WrapError(result.Error!, result.ErrorCode));
        return Ok(Wrap(result.Data));
    }

    [HttpPost("branches")]
    [Authorize(Policy = Policies.BranchCreate)]
    public async Task<IActionResult> CreateBranch([FromBody] CreateBranchCommand command, CancellationToken ct)
    {
        var result = await _mediator.Send(command, ct);
        return result.IsSuccess
            ? CreatedAtAction(nameof(GetBranch), new { id = result.Data }, Wrap(new { id = result.Data }))
            : BadRequest(WrapError(result.Error!, result.ErrorCode));
    }

    [HttpPut("branches/{id:guid}")]
    [Authorize(Policy = Policies.BranchUpdate)]
    public async Task<IActionResult> UpdateBranch(Guid id, [FromBody] UpdateBranchRequest request, CancellationToken ct)
    {
        var result = await _mediator.Send(new UpdateBranchCommand(
            id,
            request.Name,
            request.Code,
            request.Address,
            request.City,
            request.Phone,
            request.IsActive), ct);

        return result.IsSuccess
            ? Ok(Wrap(new { message = "Şube güncellendi." }))
            : BadRequest(WrapError(result.Error!, result.ErrorCode));
    }

    [HttpDelete("branches/{id:guid}")]
    [Authorize(Policy = Policies.BranchDelete)]
    public async Task<IActionResult> DeleteBranch(Guid id, CancellationToken ct)
    {
        var result = await _mediator.Send(new DeleteBranchCommand(id), ct);
        if (!result.IsSuccess) return BadRequest(WrapError(result.Error!, result.ErrorCode));
        return Ok(Wrap(new { message = "Şube silindi." }));
    }

    private static object Wrap<T>(T data) => new { success = true, data, errors = (object?)null };

    private static object WrapError(string message, string? code) =>
        new { success = false, data = (object?)null, errors = new[] { new { code = code ?? "ERROR", message } } };
}

public record UpdateBranchRequest(
    string Name,
    string Code,
    string? Address,
    string? City,
    string? Phone,
    bool IsActive);
