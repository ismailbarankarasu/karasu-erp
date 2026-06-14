using Karasu.ERP.Application.Common.Interfaces;
using Karasu.ERP.Domain.Entities;
using Karasu.ERP.Shared.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Karasu.ERP.Application.Features.Suppliers.Commands.CreateSupplier;

public record CreateSupplierCommand(
    string Name,
    string? TaxNumber,
    string? ContactPerson,
    string? Phone,
    string? Email,
    string? Address) : IRequest<Result<Guid>>;

public class CreateSupplierCommandHandler : IRequestHandler<CreateSupplierCommand, Result<Guid>>
{
    private readonly IApplicationDbContext _context;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ITenantContext _tenantContext;

    public CreateSupplierCommandHandler(
        IApplicationDbContext context,
        IUnitOfWork unitOfWork,
        ITenantContext tenantContext)
    {
        _context = context;
        _unitOfWork = unitOfWork;
        _tenantContext = tenantContext;
    }

    public async Task<Result<Guid>> Handle(CreateSupplierCommand request, CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(request.TaxNumber))
        {
            var taxExists = await _context.Suppliers
                .AnyAsync(s => s.TenantId == _tenantContext.TenantId &&
                               s.TaxNumber == request.TaxNumber &&
                               !s.IsDeleted, cancellationToken);
            if (taxExists)
                return Result<Guid>.Failure("Bu vergi numarası zaten kayıtlı.", "TAX_NUMBER_EXISTS");
        }

        var supplier = new Supplier
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantContext.TenantId,
            Name = request.Name,
            TaxNumber = request.TaxNumber,
            ContactPerson = request.ContactPerson,
            Phone = request.Phone,
            Email = request.Email,
            Address = request.Address,
            Balance = 0,
            Rating = 0,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        _context.Suppliers.Add(supplier);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<Guid>.Success(supplier.Id);
    }
}
