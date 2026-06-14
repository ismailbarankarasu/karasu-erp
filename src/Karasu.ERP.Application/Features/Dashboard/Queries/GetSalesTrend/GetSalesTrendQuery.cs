using Karasu.ERP.Application.Features.Dashboard.Common;
using Karasu.ERP.Shared.Models;
using MediatR;

namespace Karasu.ERP.Application.Features.Dashboard.Queries.GetSalesTrend;

public record GetSalesTrendQuery(
    SalesTrendPeriod Period,
    DateTime? FromDate = null,
    DateTime? ToDate = null,
    Guid? BranchId = null) : IRequest<Result<List<SalesTrendItemDto>>>;

public record SalesTrendItemDto(
    string PeriodLabel,
    DateTime Date,
    decimal TotalSales,
    int OrderCount);
