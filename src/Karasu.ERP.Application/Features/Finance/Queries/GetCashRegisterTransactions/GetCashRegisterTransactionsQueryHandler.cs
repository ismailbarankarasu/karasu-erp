using Karasu.ERP.Application.Common.Interfaces;
using Karasu.ERP.Shared.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Karasu.ERP.Application.Features.Finance.Queries.GetCashRegisterTransactions;

public class GetCashRegisterTransactionsQueryHandler
    : IRequestHandler<GetCashRegisterTransactionsQuery, Result<PaginatedList<CashTransactionDto>>>
{
    private readonly IApplicationDbContext _context;
    private readonly ITenantContext _tenantContext;

    public GetCashRegisterTransactionsQueryHandler(IApplicationDbContext context, ITenantContext tenantContext)
    {
        _context = context;
        _tenantContext = tenantContext;
    }

    public async Task<Result<PaginatedList<CashTransactionDto>>> Handle(
        GetCashRegisterTransactionsQuery request,
        CancellationToken cancellationToken)
    {
        var cashRegisterExists = await _context.CashRegisters.AnyAsync(
            c => c.Id == request.CashRegisterId &&
                 c.TenantId == _tenantContext.TenantId &&
                 !c.IsDeleted,
            cancellationToken);

        if (!cashRegisterExists)
            return Result<PaginatedList<CashTransactionDto>>.Failure("Kasa bulunamadı.", "CASH_REGISTER_NOT_FOUND");

        var query = _context.CashTransactions
            .AsNoTracking()
            .Where(t => t.CashRegisterId == request.CashRegisterId &&
                        t.TenantId == _tenantContext.TenantId &&
                        !t.IsDeleted);

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(t => t.CreatedAt)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(t => new CashTransactionDto(
                t.Id,
                t.CashRegisterId,
                t.Type,
                t.Amount,
                t.Description,
                t.ReferenceType,
                t.ReferenceId,
                t.CreatedAt))
            .ToListAsync(cancellationToken);

        return Result<PaginatedList<CashTransactionDto>>.Success(
            new PaginatedList<CashTransactionDto>(items, totalCount, request.Page, request.PageSize));
    }
}
