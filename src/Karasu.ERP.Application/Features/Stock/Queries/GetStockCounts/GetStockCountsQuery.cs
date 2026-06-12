using Karasu.ERP.Domain.Enums;
using Karasu.ERP.Shared.Models;
using MediatR;

namespace Karasu.ERP.Application.Features.Stock.Queries.GetStockCounts;

public record GetStockCountsQuery(
    int Page = 1,
    int PageSize = 20,
    Guid? WarehouseId = null,
    StockCountStatus? Status = null) : IRequest<Result<PaginatedList<StockCountListDto>>>;

public record StockCountListDto(
    Guid Id,
    Guid WarehouseId,
    string WarehouseName,
    StockCountStatus Status,
    Guid CountedBy,
    DateTime CreatedAt,
    DateTime? CompletedAt,
    int LineCount);
