using Karasu.ERP.Domain.Enums;
using Karasu.ERP.Shared.Models;
using MediatR;

namespace Karasu.ERP.Application.Features.Stock.Queries.GetStockMovements;

public record GetStockMovementsQuery(
    int Page = 1,
    int PageSize = 20,
    Guid? WarehouseId = null,
    Guid? ProductVariantId = null) : IRequest<Result<PaginatedList<StockMovementDto>>>;

public record StockMovementDto(
    Guid Id,
    Guid StockItemId,
    string ProductName,
    string Sku,
    string WarehouseName,
    StockMovementType Type,
    decimal Quantity,
    string? ReferenceType,
    Guid? ReferenceId,
    string? Note,
    DateTime CreatedAt);
