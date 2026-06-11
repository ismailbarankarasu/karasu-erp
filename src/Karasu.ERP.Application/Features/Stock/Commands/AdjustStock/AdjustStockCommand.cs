using Karasu.ERP.Shared.Models;
using MediatR;

namespace Karasu.ERP.Application.Features.Stock.Commands.AdjustStock;

public record AdjustStockCommand(
    Guid WarehouseId,
    Guid ProductVariantId,
    decimal QuantityDelta,
    string? Note) : IRequest<Result>;
