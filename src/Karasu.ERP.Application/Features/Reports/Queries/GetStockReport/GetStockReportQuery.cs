using Karasu.ERP.Shared.Models;
using MediatR;

namespace Karasu.ERP.Application.Features.Reports.Queries.GetStockReport;

public record GetStockReportQuery(Guid? WarehouseId = null) : IRequest<Result<StockReportDto>>;

public record StockReportDto(IReadOnlyList<StockReportItemDto> Items);

public record StockReportItemDto(
    string WarehouseName,
    string ProductName,
    string Sku,
    decimal Quantity,
    decimal MinStock,
    decimal Available);
