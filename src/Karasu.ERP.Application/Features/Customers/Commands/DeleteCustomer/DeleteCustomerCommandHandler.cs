using Karasu.ERP.Application.Common.Interfaces;
using Karasu.ERP.Domain.Enums;
using Karasu.ERP.Shared.Models;
using MediatR;

namespace Karasu.ERP.Application.Features.Customers.Commands.DeleteCustomer;

public class DeleteCustomerCommandHandler : IRequestHandler<DeleteCustomerCommand, Result>
{
    private readonly ICustomerRepository _customerRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ITenantContext _tenantContext;
    private readonly ICacheService _cacheService;

    public DeleteCustomerCommandHandler(
        ICustomerRepository customerRepository,
        IUnitOfWork unitOfWork,
        ITenantContext tenantContext,
        ICacheService cacheService)
    {
        _customerRepository = customerRepository;
        _unitOfWork = unitOfWork;
        _tenantContext = tenantContext;
        _cacheService = cacheService;
    }

    public async Task<Result> Handle(DeleteCustomerCommand request, CancellationToken cancellationToken)
    {
        var customer = await _customerRepository.GetByIdAsync(request.Id, cancellationToken);
        if (customer is null || customer.TenantId != _tenantContext.TenantId)
            return Result.Failure("Müşteri bulunamadı.", "CUSTOMER_NOT_FOUND");

        customer.IsDeleted = true;
        customer.Status = CustomerStatus.Inactive;

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        await _cacheService.RemoveByPatternAsync($"{_tenantContext.TenantId}:customer:list:*", cancellationToken);

        return Result.Success();
    }
}
