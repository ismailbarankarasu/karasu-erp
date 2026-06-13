using Karasu.ERP.Application.Common.Interfaces;
using Karasu.ERP.Domain.Entities;
using Karasu.ERP.Shared.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Karasu.ERP.Application.Features.Products.Commands.CreateCategory;

public class CreateCategoryCommandHandler : IRequestHandler<CreateCategoryCommand, Result<Guid>>
{
    private readonly IApplicationDbContext _context;
    private readonly ITenantContext _tenantContext;

    public CreateCategoryCommandHandler(IApplicationDbContext context, ITenantContext tenantContext)
    {
        _context = context;
        _tenantContext = tenantContext;
    }

    public async Task<Result<Guid>> Handle(CreateCategoryCommand request, CancellationToken cancellationToken)
    {
        if (request.ParentId.HasValue)
        {
            var parentExists = await _context.Categories
                .AnyAsync(
                    c => c.Id == request.ParentId.Value &&
                         c.TenantId == _tenantContext.TenantId &&
                         !c.IsDeleted,
                    cancellationToken);

            if (!parentExists)
                return Result<Guid>.Failure("Üst kategori bulunamadı.", "PARENT_CATEGORY_NOT_FOUND");
        }

        var nameExists = await _context.Categories
            .AnyAsync(
                c => c.TenantId == _tenantContext.TenantId &&
                     c.ParentId == request.ParentId &&
                     c.Name == request.Name &&
                     !c.IsDeleted,
                cancellationToken);

        if (nameExists)
            return Result<Guid>.Failure("Bu kategori adı zaten kullanılıyor.", "CATEGORY_NAME_EXISTS");

        var category = new Category
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantContext.TenantId,
            Name = request.Name,
            ParentId = request.ParentId,
            SortOrder = request.SortOrder,
            CreatedAt = DateTime.UtcNow
        };

        await _context.Categories.AddAsync(category, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        return Result<Guid>.Success(category.Id);
    }
}
