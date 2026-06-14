using Karasu.ERP.Shared.Models;
using MediatR;

namespace Karasu.ERP.Application.Features.Reports.Queries.GetCustomerReport;

public record GetCustomerReportQuery(
    DateTime FromDate,
    DateTime ToDate) : IRequest<Result<CustomerReportDto>>;

public record CustomerReportDto(IReadOnlyList<CustomerReportItemDto> Items);

public record CustomerReportItemDto(
    string CustomerName,
    int OrderCount,
    decimal TotalSpent);
