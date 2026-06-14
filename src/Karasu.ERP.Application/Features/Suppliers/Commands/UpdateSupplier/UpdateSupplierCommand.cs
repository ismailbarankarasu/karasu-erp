using Karasu.ERP.Application.Common.Interfaces;
using Karasu.ERP.Shared.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Karasu.ERP.Application.Features.Suppliers.Commands.UpdateSupplier;

public record UpdateSupplierCommand(
    Guid Id,
    string Name,
    string? TaxNumber,
    string? ContactPerson,
    string? Phone,
    string? Email,
    string? Address,
    bool IsActive) : IRequest<Result>;

public class UpdateSupplierCommandHandler : IRequestHandler<UpdateSupplierCommand, Result>
{
    private readonly IApplicationDbContext _context;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ITenantContext _tenantContext;

    public UpdateSupplierCommandHandler(
        IApplicationDbContext context,
        IUnitOfWork unitOfWork,
        ITenantContext tenantContext)
    {
        _context = context;
        _unitOfWork = unitOfWork;
        _tenantContext = tenantContext;
    }

    public async Task<Result> Handle(UpdateSupplierCommand request, CancellationToken cancellationToken)
    {
        var supplier = await _context.Suppliers
            .FirstOrDefaultAsync(s => s.Id == request.Id &&
                                      s.TenantId == _tenantContext.TenantId &&
                                      !s.IsDeleted, cancellationToken);

        if (supplier is null)
            return Result.Failure("Tedarikçi bulunamadı.", "SUPPLIER_NOT_FOUND");

        if (!string.IsNullOrWhiteSpace(request.TaxNumber) && request.TaxNumber != supplier.TaxNumber)
        {
            var taxExists = await _context.Suppliers
                .AnyAsync(s => s.TenantId == _tenantContext.TenantId &&
                               s.TaxNumber == request.TaxNumber &&
                               s.Id != request.Id &&
                               !s.IsDeleted, cancellationToken);
            if (taxExists)
                return Result.Failure("Bu vergi numarası zaten kayıtlı.", "TAX_NUMBER_EXISTS");
        }

        supplier.Name = request.Name;
        supplier.TaxNumber = request.TaxNumber;
        supplier.ContactPerson = request.ContactPerson;
        supplier.Phone = request.Phone;
        supplier.Email = request.Email;
        supplier.Address = request.Address;
        supplier.IsActive = request.IsActive;
        supplier.UpdatedAt = DateTime.UtcNow;

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
