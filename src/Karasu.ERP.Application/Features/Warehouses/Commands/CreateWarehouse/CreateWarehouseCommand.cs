using Karasu.ERP.Shared.Models;
using MediatR;

namespace Karasu.ERP.Application.Features.Warehouses.Commands.CreateWarehouse;

public record CreateWarehouseCommand(
    Guid BranchId,
    string Name,
    string Code,
    bool IsDefault = false) : IRequest<Result<Guid>>;
