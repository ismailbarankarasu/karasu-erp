using Karasu.ERP.Api.Authorization;
using Karasu.ERP.Api.Configuration;
using Karasu.ERP.Application.Features.Hr.Commands.ApproveLeaveRequest;
using Karasu.ERP.Application.Features.Hr.Commands.CreateEmployee;
using Karasu.ERP.Application.Features.Hr.Commands.CreateLeaveRequest;
using Karasu.ERP.Application.Features.Hr.Commands.CreateShift;
using Karasu.ERP.Application.Features.Hr.Commands.GeneratePayroll;
using Karasu.ERP.Application.Features.Hr.Commands.UpdateEmployee;
using Karasu.ERP.Application.Features.Hr.Queries.GetEmployeeById;
using Karasu.ERP.Application.Features.Hr.Queries.GetEmployees;
using Karasu.ERP.Application.Features.Hr.Queries.GetLeaveRequests;
using Karasu.ERP.Application.Features.Hr.Queries.GetPayrolls;
using Karasu.ERP.Application.Features.Hr.Queries.GetShifts;
using Karasu.ERP.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Karasu.ERP.Api.Controllers;

[ApiController]
[Authorize]
[EnableRateLimiting(RateLimitingConfiguration.ApiPolicy)]
[Route("api/v1/hr")]
public class HrController : ControllerBase
{
    private readonly IMediator _mediator;

    public HrController(IMediator mediator) => _mediator = mediator;

    [HttpGet("employees")]
    [Authorize(Policy = Policies.HrView)]
    public async Task<IActionResult> GetEmployees(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? search = null,
        [FromQuery] EmployeeStatus? status = null,
        CancellationToken ct = default)
    {
        var result = await _mediator.Send(new GetEmployeesQuery(page, pageSize, search, status), ct);
        return result.IsSuccess ? Ok(Wrap(result.Data)) : BadRequest(WrapError(result.Error!, result.ErrorCode));
    }

    [HttpGet("employees/{id:guid}")]
    [Authorize(Policy = Policies.HrView)]
    public async Task<IActionResult> GetEmployee(Guid id, CancellationToken ct)
    {
        var result = await _mediator.Send(new GetEmployeeByIdQuery(id), ct);
        return result.IsSuccess ? Ok(Wrap(result.Data)) : NotFound(WrapError(result.Error!, result.ErrorCode));
    }

    [HttpPost("employees")]
    [Authorize(Policy = Policies.HrCreate)]
    public async Task<IActionResult> CreateEmployee([FromBody] CreateEmployeeCommand command, CancellationToken ct)
    {
        var result = await _mediator.Send(command, ct);
        return result.IsSuccess
            ? CreatedAtAction(nameof(GetEmployee), new { id = result.Data }, Wrap(new { id = result.Data }))
            : BadRequest(WrapError(result.Error!, result.ErrorCode));
    }

    [HttpPut("employees/{id:guid}")]
    [Authorize(Policy = Policies.HrUpdate)]
    public async Task<IActionResult> UpdateEmployee(Guid id, [FromBody] UpdateEmployeeRequest request, CancellationToken ct)
    {
        var result = await _mediator.Send(new UpdateEmployeeCommand(
            id, request.FullName, request.Department, request.Position,
            request.Phone, request.Email, request.Salary, request.Status), ct);

        return result.IsSuccess
            ? Ok(Wrap(new { message = "Personel güncellendi." }))
            : BadRequest(WrapError(result.Error!, result.ErrorCode));
    }

    [HttpGet("leave-requests")]
    [Authorize(Policy = Policies.HrView)]
    public async Task<IActionResult> GetLeaveRequests(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] Guid? employeeId = null,
        [FromQuery] LeaveRequestStatus? status = null,
        CancellationToken ct = default)
    {
        var result = await _mediator.Send(new GetLeaveRequestsQuery(page, pageSize, employeeId, status), ct);
        return result.IsSuccess ? Ok(Wrap(result.Data)) : BadRequest(WrapError(result.Error!, result.ErrorCode));
    }

    [HttpPost("leave-requests")]
    [Authorize(Policy = Policies.HrCreate)]
    public async Task<IActionResult> CreateLeaveRequest([FromBody] CreateLeaveRequestCommand command, CancellationToken ct)
    {
        var result = await _mediator.Send(command, ct);
        return result.IsSuccess
            ? Ok(Wrap(new { id = result.Data }))
            : BadRequest(WrapError(result.Error!, result.ErrorCode));
    }

    [HttpPatch("leave-requests/{id:guid}/approve")]
    [Authorize(Policy = Policies.HrApprove)]
    public async Task<IActionResult> ApproveLeaveRequest(Guid id, [FromBody] ApproveLeaveRequestBody body, CancellationToken ct)
    {
        var result = await _mediator.Send(new ApproveLeaveRequestCommand(id, body.Approve), ct);
        return result.IsSuccess
            ? Ok(Wrap(new { message = body.Approve ? "İzin onaylandı." : "İzin reddedildi." }))
            : BadRequest(WrapError(result.Error!, result.ErrorCode));
    }

    [HttpGet("shifts")]
    [Authorize(Policy = Policies.HrView)]
    public async Task<IActionResult> GetShifts(
        [FromQuery] Guid? branchId = null,
        [FromQuery] DateTime? from = null,
        [FromQuery] DateTime? to = null,
        [FromQuery] Guid? employeeId = null,
        CancellationToken ct = default)
    {
        var result = await _mediator.Send(new GetShiftsQuery(branchId, from, to, employeeId), ct);
        return result.IsSuccess ? Ok(Wrap(result.Data)) : BadRequest(WrapError(result.Error!, result.ErrorCode));
    }

    [HttpPost("shifts")]
    [Authorize(Policy = Policies.HrCreate)]
    public async Task<IActionResult> CreateShift([FromBody] CreateShiftCommand command, CancellationToken ct)
    {
        var result = await _mediator.Send(command, ct);
        return result.IsSuccess
            ? Ok(Wrap(new { id = result.Data }))
            : BadRequest(WrapError(result.Error!, result.ErrorCode));
    }

    [HttpGet("payrolls")]
    [Authorize(Policy = Policies.HrView)]
    public async Task<IActionResult> GetPayrolls(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? period = null,
        [FromQuery] Guid? employeeId = null,
        CancellationToken ct = default)
    {
        var result = await _mediator.Send(new GetPayrollsQuery(page, pageSize, period, employeeId), ct);
        return result.IsSuccess ? Ok(Wrap(result.Data)) : BadRequest(WrapError(result.Error!, result.ErrorCode));
    }

    [HttpPost("payrolls/generate")]
    [Authorize(Policy = Policies.HrCreate)]
    public async Task<IActionResult> GeneratePayroll([FromBody] GeneratePayrollCommand command, CancellationToken ct)
    {
        var result = await _mediator.Send(command, ct);
        return result.IsSuccess ? Ok(Wrap(result.Data)) : BadRequest(WrapError(result.Error!, result.ErrorCode));
    }

    private static object Wrap<T>(T data) => new { success = true, data, errors = (object?)null };

    private static object WrapError(string message, string? code) =>
        new { success = false, data = (object?)null, errors = new[] { new { code = code ?? "ERROR", message } } };
}

public record UpdateEmployeeRequest(
    string FullName,
    string? Department,
    string? Position,
    string? Phone,
    string? Email,
    decimal Salary,
    EmployeeStatus Status);

public record ApproveLeaveRequestBody(bool Approve = true);
