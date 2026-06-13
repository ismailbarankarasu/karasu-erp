using Karasu.ERP.Application.Common.Interfaces;
using Karasu.ERP.Shared.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Karasu.ERP.Application.Features.Finance.Queries.GetBankAccountTransactions;

public class GetBankAccountTransactionsQueryHandler
    : IRequestHandler<GetBankAccountTransactionsQuery, Result<PaginatedList<BankTransactionDto>>>
{
    private readonly IApplicationDbContext _context;
    private readonly ITenantContext _tenantContext;

    public GetBankAccountTransactionsQueryHandler(IApplicationDbContext context, ITenantContext tenantContext)
    {
        _context = context;
        _tenantContext = tenantContext;
    }

    public async Task<Result<PaginatedList<BankTransactionDto>>> Handle(
        GetBankAccountTransactionsQuery request,
        CancellationToken cancellationToken)
    {
        var bankAccountExists = await _context.BankAccounts.AnyAsync(
            b => b.Id == request.BankAccountId &&
                 b.TenantId == _tenantContext.TenantId &&
                 !b.IsDeleted,
            cancellationToken);

        if (!bankAccountExists)
            return Result<PaginatedList<BankTransactionDto>>.Failure("Banka hesabı bulunamadı.", "BANK_ACCOUNT_NOT_FOUND");

        var query = _context.BankTransactions
            .AsNoTracking()
            .Where(t => t.BankAccountId == request.BankAccountId &&
                        t.TenantId == _tenantContext.TenantId &&
                        !t.IsDeleted);

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(t => t.CreatedAt)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(t => new BankTransactionDto(
                t.Id,
                t.BankAccountId,
                t.Type,
                t.Amount,
                t.Description,
                t.ReferenceNo,
                t.CreatedAt))
            .ToListAsync(cancellationToken);

        return Result<PaginatedList<BankTransactionDto>>.Success(
            new PaginatedList<BankTransactionDto>(items, totalCount, request.Page, request.PageSize));
    }
}
