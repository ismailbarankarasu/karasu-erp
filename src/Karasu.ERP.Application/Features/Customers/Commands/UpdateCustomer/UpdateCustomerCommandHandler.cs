using Karasu.ERP.Application.Common.Interfaces;
using Karasu.ERP.Shared.Models;
using MediatR;

namespace Karasu.ERP.Application.Features.Customers.Commands.UpdateCustomer;

public class UpdateCustomerCommandHandler : IRequestHandler<UpdateCustomerCommand, Result>
{
    private readonly ICustomerRepository _customerRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ITenantContext _tenantContext;
    private readonly ICacheService _cacheService;

    public UpdateCustomerCommandHandler(
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

    public async Task<Result> Handle(UpdateCustomerCommand request, CancellationToken cancellationToken)
    {
        var customer = await _customerRepository.GetByIdAsync(request.Id, cancellationToken);
        if (customer is null || customer.TenantId != _tenantContext.TenantId)
            return Result.Failure("Müşteri bulunamadı.", "CUSTOMER_NOT_FOUND");

        if (!string.IsNullOrWhiteSpace(request.TaxNumber) &&
            await _customerRepository.TaxNumberExistsAsync(request.TaxNumber, request.Id, cancellationToken))
            return Result.Failure("Bu vergi numarası zaten kayıtlı.", "TAX_NUMBER_EXISTS");

        customer.Type = request.Type;
        customer.FullName = request.FullName;
        customer.CompanyName = request.CompanyName;
        customer.TaxNumber = request.TaxNumber;
        customer.Phone = request.Phone;
        customer.Email = request.Email;
        customer.Address = request.Address;
        customer.City = request.City;
        customer.CreditLimit = request.CreditLimit;
        customer.Status = request.Status;

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        await _cacheService.RemoveByPatternAsync($"{_tenantContext.TenantId}:customer:list:*", cancellationToken);

        return Result.Success();
    }
}
