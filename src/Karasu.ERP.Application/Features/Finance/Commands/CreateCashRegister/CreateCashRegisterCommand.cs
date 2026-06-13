using Karasu.ERP.Shared.Models;
using MediatR;

namespace Karasu.ERP.Application.Features.Finance.Commands.CreateCashRegister;

public record CreateCashRegisterCommand(
    Guid BranchId,
    string Name,
    decimal? OpeningBalance = null) : IRequest<Result<Guid>>;
