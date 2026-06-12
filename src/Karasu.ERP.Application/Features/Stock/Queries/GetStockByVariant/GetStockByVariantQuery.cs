using Karasu.ERP.Shared.Models;
using MediatR;

namespace Karasu.ERP.Application.Features.Stock.Queries.GetStockByVariant;

public record GetStockByVariantQuery(Guid ProductVariantId) : IRequest<Result<StockByVariantDto>>;

public record StockByVariantDto(
    Guid ProductVariantId,
    string ProductName,
    string Sku,
    decimal TotalQuantity,
    decimal TotalAvailable,
    IReadOnlyList<StockByWarehouseDto> Warehouses);

public record StockByWarehouseDto(
    Guid WarehouseId,
    string WarehouseName,
    decimal Quantity,
    decimal ReservedQuantity,
    decimal AvailableQuantity,
    decimal MinStock);
