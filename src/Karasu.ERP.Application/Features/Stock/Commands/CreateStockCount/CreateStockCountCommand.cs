using Karasu.ERP.Shared.Models;
using MediatR;

namespace Karasu.ERP.Application.Features.Stock.Commands.CreateStockCount;

public record CreateStockCountCommand(
    Guid WarehouseId,
    string? Note) : IRequest<Result<Guid>>;
