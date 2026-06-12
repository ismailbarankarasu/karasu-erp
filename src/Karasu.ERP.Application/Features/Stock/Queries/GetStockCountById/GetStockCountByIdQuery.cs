using Karasu.ERP.Domain.Enums;
using Karasu.ERP.Shared.Models;
using MediatR;

namespace Karasu.ERP.Application.Features.Stock.Queries.GetStockCountById;

public record GetStockCountByIdQuery(Guid Id) : IRequest<Result<StockCountDetailDto>>;

public record StockCountDetailDto(
    Guid Id,
    Guid WarehouseId,
    string WarehouseName,
    StockCountStatus Status,
    Guid CountedBy,
    DateTime CreatedAt,
    DateTime? CompletedAt,
    string? Note,
    IReadOnlyList<StockCountLineDto> Lines);

public record StockCountLineDto(
    Guid Id,
    Guid ProductVariantId,
    string ProductName,
    string Sku,
    decimal SystemQty,
    decimal? CountedQty,
    decimal Difference);
