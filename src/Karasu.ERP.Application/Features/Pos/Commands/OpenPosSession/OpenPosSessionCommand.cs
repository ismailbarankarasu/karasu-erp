using Karasu.ERP.Shared.Models;
using MediatR;

namespace Karasu.ERP.Application.Features.Pos.Commands.OpenPosSession;

public record OpenPosSessionCommand(Guid BranchId, decimal OpeningBalance) : IRequest<Result<Guid>>;
