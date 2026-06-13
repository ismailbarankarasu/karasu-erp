using Karasu.ERP.Application.Common.Interfaces;
using Karasu.ERP.Shared.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Karasu.ERP.Application.Features.Finance.Queries.GetCashRegisters;

public class GetCashRegistersQueryHandler : IRequestHandler<GetCashRegistersQuery, Result<List<CashRegisterDto>>>
{
    private readonly IApplicationDbContext _context;
    private readonly ITenantContext _tenantContext;

    public GetCashRegistersQueryHandler(IApplicationDbContext context, ITenantContext tenantContext)
    {
        _context = context;
        _tenantContext = tenantContext;
    }

    public async Task<Result<List<CashRegisterDto>>> Handle(
        GetCashRegistersQuery request,
        CancellationToken cancellationToken)
    {
        var query = _context.CashRegisters
            .AsNoTracking()
            .Where(c => c.TenantId == _tenantContext.TenantId && !c.IsDeleted);

        if (request.BranchId.HasValue)
            query = query.Where(c => c.BranchId == request.BranchId.Value);

        var items = await query
            .OrderBy(c => c.Name)
            .Select(c => new CashRegisterDto(
                c.Id,
                c.BranchId,
                c.Branch.Name,
                c.Name,
                c.CurrentBalance,
                c.IsActive))
            .ToListAsync(cancellationToken);

        return Result<List<CashRegisterDto>>.Success(items);
    }
}
