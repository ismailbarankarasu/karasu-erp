using Karasu.ERP.Shared.Models;
using MediatR;

namespace Karasu.ERP.Application.Features.Orders.Queries.GetBranches;

public record GetBranchesQuery : IRequest<Result<IReadOnlyList<BranchDto>>>;

public record BranchDto(Guid Id, string Name, string Code, bool IsActive);
