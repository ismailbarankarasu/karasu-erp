using Karasu.ERP.Shared.Models;
using MediatR;

namespace Karasu.ERP.Application.Features.Stock.Queries.GetStockAlerts;

public record GetStockAlertsQuery(Guid? WarehouseId = null) : IRequest<Result<List<StockAlertDto>>>;

public record StockAlertDto(
    Guid StockItemId,
    Guid WarehouseId,
    string WarehouseName,
    Guid ProductVariantId,
    string ProductName,
    string Sku,
    decimal Quantity,
    decimal MinStock,
    decimal Shortage);
