using Karasu.ERP.Application.Common.Interfaces;
using Karasu.ERP.Shared.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Karasu.ERP.Application.Features.Customers.Queries.GetCustomerById;

public class GetCustomerByIdQueryHandler : IRequestHandler<GetCustomerByIdQuery, Result<CustomerDetailDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ITenantContext _tenantContext;

    public GetCustomerByIdQueryHandler(IApplicationDbContext context, ITenantContext tenantContext)
    {
        _context = context;
        _tenantContext = tenantContext;
    }

    public async Task<Result<CustomerDetailDto>> Handle(
        GetCustomerByIdQuery request,
        CancellationToken cancellationToken)
    {
        var customer = await _context.Customers
            .AsNoTracking()
            .Where(c => c.Id == request.Id && c.TenantId == _tenantContext.TenantId && !c.IsDeleted)
            .Select(c => new CustomerDetailDto(
                c.Id,
                c.Type,
                c.FullName,
                c.CompanyName,
                c.TaxNumber,
                c.Phone,
                c.Email,
                c.Address,
                c.City,
                c.Balance,
                c.CreditLimit,
                c.Status,
                c.CreatedAt,
                c.UpdatedAt))
            .FirstOrDefaultAsync(cancellationToken);

        if (customer is null)
            return Result<CustomerDetailDto>.Failure("Müşteri bulunamadı.", "CUSTOMER_NOT_FOUND");

        return Result<CustomerDetailDto>.Success(customer);
    }
}
