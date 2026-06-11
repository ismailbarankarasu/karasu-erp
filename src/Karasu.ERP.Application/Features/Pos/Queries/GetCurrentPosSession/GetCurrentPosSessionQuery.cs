using Karasu.ERP.Domain.Enums;
using Karasu.ERP.Shared.Models;
using MediatR;

namespace Karasu.ERP.Application.Features.Pos.Queries.GetCurrentPosSession;

public record GetCurrentPosSessionQuery : IRequest<Result<CurrentPosSessionDto?>>;

public record CurrentPosSessionDto(
    Guid Id,
    Guid BranchId,
    string BranchName,
    DateTime OpenedAt,
    decimal OpeningBalance,
    PosSessionStatus Status,
    int SaleCount,
    decimal TotalSales,
    decimal CashSales);
