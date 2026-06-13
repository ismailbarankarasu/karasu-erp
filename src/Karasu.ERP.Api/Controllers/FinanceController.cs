using Karasu.ERP.Api.Authorization;
using Karasu.ERP.Api.Configuration;
using Karasu.ERP.Application.Features.Finance.Commands.CreateBankAccount;
using Karasu.ERP.Application.Features.Finance.Commands.CreateBankTransaction;
using Karasu.ERP.Application.Features.Finance.Commands.CreateCashRegister;
using Karasu.ERP.Application.Features.Finance.Commands.CreateCashTransaction;
using Karasu.ERP.Application.Features.Finance.Commands.CreateExpense;
using Karasu.ERP.Application.Features.Finance.Commands.CreateFinancePayment;
using Karasu.ERP.Application.Features.Finance.Commands.CreateIncome;
using Karasu.ERP.Application.Features.Finance.Queries.GetBankAccounts;
using Karasu.ERP.Application.Features.Finance.Queries.GetBankAccountTransactions;
using Karasu.ERP.Application.Features.Finance.Queries.GetCashRegisters;
using Karasu.ERP.Application.Features.Finance.Queries.GetCashRegisterTransactions;
using Karasu.ERP.Application.Features.Finance.Queries.GetExpenses;
using Karasu.ERP.Application.Features.Finance.Queries.GetFinanceSummary;
using Karasu.ERP.Application.Features.Finance.Queries.GetIncomes;
using Karasu.ERP.Application.Features.Finance.Queries.GetPayables;
using Karasu.ERP.Application.Features.Finance.Queries.GetReceivables;
using Karasu.ERP.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Karasu.ERP.Api.Controllers;

[ApiController]
[Authorize]
[EnableRateLimiting(RateLimitingConfiguration.ApiPolicy)]
[Route("api/v1/finance")]
public class FinanceController : ControllerBase
{
    private readonly IMediator _mediator;

    public FinanceController(IMediator mediator) => _mediator = mediator;

    [HttpGet("cash-registers")]
    [Authorize(Policy = Policies.FinanceView)]
    public async Task<IActionResult> GetCashRegisters([FromQuery] Guid? branchId, CancellationToken ct)
    {
        var result = await _mediator.Send(new GetCashRegistersQuery(branchId), ct);
        return result.IsSuccess ? Ok(Wrap(result.Data)) : BadRequest(WrapError(result.Error!, result.ErrorCode));
    }

    [HttpPost("cash-registers")]
    [Authorize(Policy = Policies.FinanceCreate)]
    public async Task<IActionResult> CreateCashRegister([FromBody] CreateCashRegisterCommand command, CancellationToken ct)
    {
        var result = await _mediator.Send(command, ct);
        return result.IsSuccess
            ? Ok(Wrap(new { id = result.Data }))
            : BadRequest(WrapError(result.Error!, result.ErrorCode));
    }

    [HttpGet("cash-registers/{id:guid}/transactions")]
    [Authorize(Policy = Policies.FinanceView)]
    public async Task<IActionResult> GetCashRegisterTransactions(
        Guid id,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var result = await _mediator.Send(new GetCashRegisterTransactionsQuery(id, page, pageSize), ct);
        if (!result.IsSuccess) return NotFound(WrapError(result.Error!, result.ErrorCode));
        return Ok(Wrap(result.Data));
    }

    [HttpPost("cash-transactions")]
    [Authorize(Policy = Policies.FinanceCreate)]
    public async Task<IActionResult> CreateCashTransaction([FromBody] CreateCashTransactionCommand command, CancellationToken ct)
    {
        var result = await _mediator.Send(command, ct);
        return result.IsSuccess
            ? Ok(Wrap(new { id = result.Data }))
            : BadRequest(WrapError(result.Error!, result.ErrorCode));
    }

    [HttpGet("bank-accounts")]
    [Authorize(Policy = Policies.FinanceView)]
    public async Task<IActionResult> GetBankAccounts(CancellationToken ct)
    {
        var result = await _mediator.Send(new GetBankAccountsQuery(), ct);
        return result.IsSuccess ? Ok(Wrap(result.Data)) : BadRequest(WrapError(result.Error!, result.ErrorCode));
    }

    [HttpPost("bank-accounts")]
    [Authorize(Policy = Policies.FinanceCreate)]
    public async Task<IActionResult> CreateBankAccount([FromBody] CreateBankAccountCommand command, CancellationToken ct)
    {
        var result = await _mediator.Send(command, ct);
        return result.IsSuccess
            ? Ok(Wrap(new { id = result.Data }))
            : BadRequest(WrapError(result.Error!, result.ErrorCode));
    }

    [HttpGet("bank-accounts/{id:guid}/transactions")]
    [Authorize(Policy = Policies.FinanceView)]
    public async Task<IActionResult> GetBankAccountTransactions(
        Guid id,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var result = await _mediator.Send(new GetBankAccountTransactionsQuery(id, page, pageSize), ct);
        if (!result.IsSuccess) return NotFound(WrapError(result.Error!, result.ErrorCode));
        return Ok(Wrap(result.Data));
    }

    [HttpPost("bank-transactions")]
    [Authorize(Policy = Policies.FinanceCreate)]
    public async Task<IActionResult> CreateBankTransaction([FromBody] CreateBankTransactionCommand command, CancellationToken ct)
    {
        var result = await _mediator.Send(command, ct);
        return result.IsSuccess
            ? Ok(Wrap(new { id = result.Data }))
            : BadRequest(WrapError(result.Error!, result.ErrorCode));
    }

    [HttpGet("expenses")]
    [Authorize(Policy = Policies.FinanceView)]
    public async Task<IActionResult> GetExpenses(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] DateTime? fromDate = null,
        [FromQuery] DateTime? toDate = null,
        CancellationToken ct = default)
    {
        var result = await _mediator.Send(new GetExpensesQuery(page, pageSize, fromDate, toDate), ct);
        return result.IsSuccess ? Ok(Wrap(result.Data)) : BadRequest(WrapError(result.Error!, result.ErrorCode));
    }

    [HttpPost("expenses")]
    [Authorize(Policy = Policies.FinanceCreate)]
    public async Task<IActionResult> CreateExpense([FromBody] CreateExpenseCommand command, CancellationToken ct)
    {
        var result = await _mediator.Send(command, ct);
        return result.IsSuccess
            ? Ok(Wrap(new { id = result.Data }))
            : BadRequest(WrapError(result.Error!, result.ErrorCode));
    }

    [HttpGet("incomes")]
    [Authorize(Policy = Policies.FinanceView)]
    public async Task<IActionResult> GetIncomes(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] DateTime? fromDate = null,
        [FromQuery] DateTime? toDate = null,
        CancellationToken ct = default)
    {
        var result = await _mediator.Send(new GetIncomesQuery(page, pageSize, fromDate, toDate), ct);
        return result.IsSuccess ? Ok(Wrap(result.Data)) : BadRequest(WrapError(result.Error!, result.ErrorCode));
    }

    [HttpPost("incomes")]
    [Authorize(Policy = Policies.FinanceCreate)]
    public async Task<IActionResult> CreateIncome([FromBody] CreateIncomeCommand command, CancellationToken ct)
    {
        var result = await _mediator.Send(command, ct);
        return result.IsSuccess
            ? Ok(Wrap(new { id = result.Data }))
            : BadRequest(WrapError(result.Error!, result.ErrorCode));
    }

    [HttpGet("receivables")]
    [Authorize(Policy = Policies.FinanceView)]
    public async Task<IActionResult> GetReceivables(
        [FromQuery] Guid? customerId = null,
        [FromQuery] ReceivableStatus? status = null,
        CancellationToken ct = default)
    {
        var result = await _mediator.Send(new GetReceivablesQuery(customerId, status), ct);
        return result.IsSuccess ? Ok(Wrap(result.Data)) : BadRequest(WrapError(result.Error!, result.ErrorCode));
    }

    [HttpGet("payables")]
    [Authorize(Policy = Policies.FinanceView)]
    public async Task<IActionResult> GetPayables(
        [FromQuery] PayableStatus? status = null,
        CancellationToken ct = default)
    {
        var result = await _mediator.Send(new GetPayablesQuery(status), ct);
        return result.IsSuccess ? Ok(Wrap(result.Data)) : BadRequest(WrapError(result.Error!, result.ErrorCode));
    }

    [HttpPost("payments")]
    [Authorize(Policy = Policies.FinanceCreate)]
    public async Task<IActionResult> CreatePayment([FromBody] CreateFinancePaymentCommand command, CancellationToken ct)
    {
        var result = await _mediator.Send(command, ct);
        return result.IsSuccess
            ? Ok(Wrap(new { id = result.Data }))
            : BadRequest(WrapError(result.Error!, result.ErrorCode));
    }

    [HttpGet("summary")]
    [Authorize(Policy = Policies.FinanceView)]
    public async Task<IActionResult> GetSummary(CancellationToken ct)
    {
        var result = await _mediator.Send(new GetFinanceSummaryQuery(), ct);
        return result.IsSuccess ? Ok(Wrap(result.Data)) : BadRequest(WrapError(result.Error!, result.ErrorCode));
    }

    private static object Wrap<T>(T data) => new { success = true, data, errors = (object?)null };

    private static object WrapError(string message, string? code) =>
        new { success = false, data = (object?)null, errors = new[] { new { code = code ?? "ERROR", message } } };
}
