using Karasu.ERP.Shared.Models;
using MediatR;

namespace Karasu.ERP.Application.Features.Warehouses.Queries.GetWarehouses;

public record GetWarehousesQuery(Guid? BranchId = null) : IRequest<Result<List<WarehouseDto>>>;

public record WarehouseDto(
    Guid Id,
    Guid BranchId,
    string BranchName,
    string Name,
    string Code,
    bool IsDefault);
