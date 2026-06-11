using Karasu.ERP.Shared.Models;
using MediatR;

namespace Karasu.ERP.Application.Features.Pos.Commands.ClosePosSession;

public record ClosePosSessionCommand(Guid SessionId, decimal ClosingBalance) : IRequest<Result<PosSessionCloseDto>>;

public record PosSessionCloseDto(
    Guid SessionId,
    decimal OpeningBalance,
    decimal ClosingBalance,
    decimal TotalSales,
    decimal ExpectedCash,
    decimal CashVariance,
    int SaleCount);
