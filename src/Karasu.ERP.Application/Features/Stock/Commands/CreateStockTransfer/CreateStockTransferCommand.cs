using Karasu.ERP.Shared.Models;
using MediatR;

namespace Karasu.ERP.Application.Features.Stock.Commands.CreateStockTransfer;

public record CreateStockTransferCommand(
    Guid FromWarehouseId,
    Guid ToWarehouseId,
    string? Note,
    IReadOnlyList<StockTransferLineDto> Lines) : IRequest<Result<Guid>>;

public record StockTransferLineDto(Guid ProductVariantId, decimal Quantity);
