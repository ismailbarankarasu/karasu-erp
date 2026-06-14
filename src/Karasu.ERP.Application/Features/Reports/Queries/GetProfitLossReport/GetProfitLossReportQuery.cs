using Karasu.ERP.Shared.Models;
using MediatR;

namespace Karasu.ERP.Application.Features.Reports.Queries.GetProfitLossReport;

public record GetProfitLossReportQuery(
    DateTime FromDate,
    DateTime ToDate) : IRequest<Result<ProfitLossReportDto>>;

public record ProfitLossReportDto(
    decimal Revenue,
    decimal Cogs,
    decimal Expenses,
    decimal Profit);
