using Karasu.ERP.Shared.Models;
using MediatR;

namespace Karasu.ERP.Application.Features.Products.Queries.GetUnits;

public record GetUnitsQuery : IRequest<Result<IReadOnlyList<UnitDto>>>;

public record UnitDto(Guid Id, string Name, string Symbol);
