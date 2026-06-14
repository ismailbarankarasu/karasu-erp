using Karasu.ERP.Application.Common.Interfaces;
using Karasu.ERP.Domain.Entities;
using Karasu.ERP.Shared.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Karasu.ERP.Application.Features.Branches.Commands.CreateBranch;

public record CreateBranchCommand(
    string Name,
    string Code,
    string? Address,
    string? City,
    string? Phone) : IRequest<Result<Guid>>;

public class CreateBranchCommandHandler : IRequestHandler<CreateBranchCommand, Result<Guid>>
{
    private readonly IApplicationDbContext _context;
    private readonly ITenantContext _tenantContext;
    private readonly IUnitOfWork _unitOfWork;

    public CreateBranchCommandHandler(
        IApplicationDbContext context,
        ITenantContext tenantContext,
        IUnitOfWork unitOfWork)
    {
        _context = context;
        _tenantContext = tenantContext;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<Guid>> Handle(CreateBranchCommand request, CancellationToken cancellationToken)
    {
        var code = request.Code.Trim().ToUpperInvariant();
        var exists = await _context.Branches.AnyAsync(
            b => b.TenantId == _tenantContext.TenantId && b.Code == code && !b.IsDeleted,
            cancellationToken);

        if (exists)
            return Result<Guid>.Failure("Bu şube kodu zaten kullanılıyor.", "BRANCH_CODE_EXISTS");

        var branch = new Branch
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantContext.TenantId,
            Name = request.Name.Trim(),
            Code = code,
            Address = request.Address?.Trim(),
            City = request.City?.Trim(),
            Phone = request.Phone?.Trim(),
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        await _context.Branches.AddAsync(branch, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result<Guid>.Success(branch.Id);
    }
}
