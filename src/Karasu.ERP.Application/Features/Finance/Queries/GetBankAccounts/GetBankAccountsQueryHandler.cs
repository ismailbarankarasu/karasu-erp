using Karasu.ERP.Application.Common.Interfaces;
using Karasu.ERP.Shared.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Karasu.ERP.Application.Features.Finance.Queries.GetBankAccounts;

public class GetBankAccountsQueryHandler : IRequestHandler<GetBankAccountsQuery, Result<List<BankAccountDto>>>
{
    private readonly IApplicationDbContext _context;
    private readonly ITenantContext _tenantContext;

    public GetBankAccountsQueryHandler(IApplicationDbContext context, ITenantContext tenantContext)
    {
        _context = context;
        _tenantContext = tenantContext;
    }

    public async Task<Result<List<BankAccountDto>>> Handle(
        GetBankAccountsQuery request,
        CancellationToken cancellationToken)
    {
        var items = await _context.BankAccounts
            .AsNoTracking()
            .Where(b => b.TenantId == _tenantContext.TenantId && !b.IsDeleted)
            .OrderBy(b => b.BankName)
            .ThenBy(b => b.AccountName)
            .Select(b => new BankAccountDto(
                b.Id,
                b.BankName,
                b.AccountName,
                b.Iban,
                b.CurrentBalance,
                b.IsActive))
            .ToListAsync(cancellationToken);

        return Result<List<BankAccountDto>>.Success(items);
    }
}
