using Karasu.ERP.Application.Common.Interfaces;
using Karasu.ERP.Shared.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Karasu.ERP.Application.Features.Customers.Queries.GetCustomerPaymentHistory;

public class GetCustomerPaymentHistoryQueryHandler
    : IRequestHandler<GetCustomerPaymentHistoryQuery, Result<IReadOnlyList<CustomerPaymentDto>>>
{
    private readonly IApplicationDbContext _context;
    private readonly ITenantContext _tenantContext;

    public GetCustomerPaymentHistoryQueryHandler(IApplicationDbContext context, ITenantContext tenantContext)
    {
        _context = context;
        _tenantContext = tenantContext;
    }

    public async Task<Result<IReadOnlyList<CustomerPaymentDto>>> Handle(
        GetCustomerPaymentHistoryQuery request,
        CancellationToken cancellationToken)
    {
        var customerExists = await _context.Customers
            .AnyAsync(
                c => c.Id == request.CustomerId && c.TenantId == _tenantContext.TenantId && !c.IsDeleted,
                cancellationToken);

        if (!customerExists)
            return Result<IReadOnlyList<CustomerPaymentDto>>.Failure("Müşteri bulunamadı.", "CUSTOMER_NOT_FOUND");

        var payments = await _context.FinancePayments
            .AsNoTracking()
            .Where(p => p.CustomerId == request.CustomerId && p.TenantId == _tenantContext.TenantId && !p.IsDeleted)
            .OrderByDescending(p => p.PaidAt)
            .Select(p => new CustomerPaymentDto(
                p.Id,
                p.Direction,
                p.Method,
                p.Amount,
                p.PaidAt,
                p.ReferenceNo,
                p.Note,
                p.OrderId,
                p.InvoiceId))
            .ToListAsync(cancellationToken);

        return Result<IReadOnlyList<CustomerPaymentDto>>.Success(payments);
    }
}
