using Karasu.ERP.Shared.Models;
using MediatR;

namespace Karasu.ERP.Application.Features.Stock.Queries.GetStockItems;

public record GetStockItemsQuery(
    int Page = 1,
    int PageSize = 20,
    Guid? WarehouseId = null,
    string? Search = null) : IRequest<Result<PaginatedList<StockItemDto>>>;

public record StockItemDto(
    Guid Id,
    Guid WarehouseId,
    string WarehouseName,
    Guid ProductVariantId,
    string ProductName,
    string Sku,
    decimal Quantity,
    decimal ReservedQuantity,
    decimal AvailableQuantity,
    decimal MinStock);
