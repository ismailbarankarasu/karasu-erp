using Karasu.ERP.Shared.Models;
using MediatR;

namespace Karasu.ERP.Application.Features.Stock.Commands.UpdateStockCountLines;

public record UpdateStockCountLinesCommand(
    Guid CountId,
    IReadOnlyList<StockCountLineUpdateDto> Lines) : IRequest<Result>;

public record StockCountLineUpdateDto(
    Guid? LineId,
    Guid? ProductVariantId,
    decimal CountedQty);
