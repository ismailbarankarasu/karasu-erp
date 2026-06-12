using Karasu.ERP.Application.Features.Warehouses.Queries.GetWarehouses;
using Karasu.ERP.Shared.Models;
using MediatR;

namespace Karasu.ERP.Application.Features.Warehouses.Queries.GetWarehouseById;

public record GetWarehouseByIdQuery(Guid Id) : IRequest<Result<WarehouseDto>>;
