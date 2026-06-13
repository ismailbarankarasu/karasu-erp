using Karasu.ERP.Application.Common.Interfaces;
using Karasu.ERP.Domain.Entities;
using Karasu.ERP.Shared.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Karasu.ERP.Application.Features.Products.Commands.CreateBrand;

public class CreateBrandCommandHandler : IRequestHandler<CreateBrandCommand, Result<Guid>>
{
    private readonly IApplicationDbContext _context;
    private readonly ITenantContext _tenantContext;

    public CreateBrandCommandHandler(IApplicationDbContext context, ITenantContext tenantContext)
    {
        _context = context;
        _tenantContext = tenantContext;
    }

    public async Task<Result<Guid>> Handle(CreateBrandCommand request, CancellationToken cancellationToken)
    {
        var nameExists = await _context.Brands
            .AnyAsync(
                b => b.TenantId == _tenantContext.TenantId && b.Name == request.Name && !b.IsDeleted,
                cancellationToken);

        if (nameExists)
            return Result<Guid>.Failure("Bu marka adı zaten kullanılıyor.", "BRAND_NAME_EXISTS");

        var brand = new Brand
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantContext.TenantId,
            Name = request.Name,
            CreatedAt = DateTime.UtcNow
        };

        await _context.Brands.AddAsync(brand, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        return Result<Guid>.Success(brand.Id);
    }
}
