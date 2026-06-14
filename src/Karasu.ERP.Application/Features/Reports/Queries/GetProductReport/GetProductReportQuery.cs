using Karasu.ERP.Shared.Models;
using MediatR;

namespace Karasu.ERP.Application.Features.Reports.Queries.GetProductReport;

public record GetProductReportQuery(
    DateTime FromDate,
    DateTime ToDate) : IRequest<Result<ProductReportDto>>;

public record ProductReportDto(IReadOnlyList<ProductReportItemDto> Items);

public record ProductReportItemDto(
    string ProductName,
    string Sku,
    decimal QuantitySold,
    decimal Revenue);
