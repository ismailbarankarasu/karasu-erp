using Karasu.ERP.Shared.Models;
using MediatR;

namespace Karasu.ERP.Application.Features.Finance.Queries.GetCashRegisters;

public record GetCashRegistersQuery(Guid? BranchId = null) : IRequest<Result<List<CashRegisterDto>>>;

public record CashRegisterDto(
    Guid Id,
    Guid BranchId,
    string BranchName,
    string Name,
    decimal CurrentBalance,
    bool IsActive);
