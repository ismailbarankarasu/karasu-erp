using Karasu.ERP.Domain.Enums;
using Karasu.ERP.Shared.Models;
using MediatR;

namespace Karasu.ERP.Application.Features.Stock.Queries.GetStockTransfers;

public record GetStockTransfersQuery(
    int Page = 1,
    int PageSize = 20,
    StockTransferStatus? Status = null) : IRequest<Result<PaginatedList<StockTransferDto>>>;

public record StockTransferDto(
    Guid Id,
    Guid FromWarehouseId,
    string FromWarehouseName,
    Guid ToWarehouseId,
    string ToWarehouseName,
    StockTransferStatus Status,
    Guid RequestedBy,
    DateTime CreatedAt,
    DateTime? CompletedAt,
    int LineCount);
