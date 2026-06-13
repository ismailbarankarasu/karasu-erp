using Karasu.ERP.Shared.Models;
using MediatR;

namespace Karasu.ERP.Application.Features.Finance.Queries.GetFinanceSummary;

public record GetFinanceSummaryQuery() : IRequest<Result<FinanceSummaryDto>>;

public record FinanceSummaryDto(
    decimal TotalCashBalance,
    decimal TotalBankBalance,
    decimal TotalReceivables,
    decimal TotalPayables,
    decimal MonthIncome,
    decimal MonthExpense);
