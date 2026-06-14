using Karasu.ERP.Shared.Models;
using MediatR;

namespace Karasu.ERP.Application.Features.Dashboard.Queries.GetDashboardCriticalStock;

public record GetDashboardCriticalStockQuery() : IRequest<Result<List<DashboardCriticalStockDto>>>;

public record DashboardCriticalStockDto(
    Guid StockItemId,
    Guid WarehouseId,
    string WarehouseName,
    Guid ProductVariantId,
    string ProductName,
    string Sku,
    decimal Quantity,
    decimal MinStock,
    decimal Shortage);
