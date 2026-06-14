using Karasu.ERP.Domain.Enums;
using Karasu.ERP.Shared.Models;
using MediatR;

namespace Karasu.ERP.Application.Features.Reports.Queries.GetSalesReport;

public record GetSalesReportQuery(
    DateTime FromDate,
    DateTime ToDate,
    Guid? BranchId = null) : IRequest<Result<SalesReportDto>>;

public record SalesReportDto(
    decimal TotalSales,
    int OrderCount,
    IReadOnlyList<SalesReportItemDto> Items);

public record SalesReportItemDto(
    string OrderNumber,
    DateTime Date,
    string BranchName,
    string? CustomerName,
    decimal GrandTotal,
    OrderStatus Status);
