using Karasu.ERP.Application.Common.Interfaces;
using Karasu.ERP.Domain.Entities;
using Karasu.ERP.Domain.Enums;
using Karasu.ERP.Shared.Models;
using MediatR;

namespace Karasu.ERP.Application.Features.Customers.Commands.CreateCustomer;

public class CreateCustomerCommandHandler : IRequestHandler<CreateCustomerCommand, Result<Guid>>
{
    private readonly ICustomerRepository _customerRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ITenantContext _tenantContext;
    private readonly ICacheService _cacheService;

    public CreateCustomerCommandHandler(
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

    public async Task<Result<Guid>> Handle(CreateCustomerCommand request, CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(request.TaxNumber) &&
            await _customerRepository.TaxNumberExistsAsync(request.TaxNumber, null, cancellationToken))
            return Result<Guid>.Failure("Bu vergi numarası zaten kayıtlı.", "TAX_NUMBER_EXISTS");

        var customer = new Customer
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantContext.TenantId,
            Type = request.Type,
            FullName = request.FullName,
            CompanyName = request.CompanyName,
            TaxNumber = request.TaxNumber,
            Phone = request.Phone,
            Email = request.Email,
            Address = request.Address,
            City = request.City,
            Balance = 0,
            CreditLimit = request.CreditLimit,
            Status = CustomerStatus.Active,
            CreatedAt = DateTime.UtcNow
        };

        await _customerRepository.AddAsync(customer, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await _cacheService.RemoveByPatternAsync($"{_tenantContext.TenantId}:customer:list:*", cancellationToken);

        return Result<Guid>.Success(customer.Id);
    }
}
