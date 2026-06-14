using Karasu.ERP.Application.Common.Interfaces;
using Karasu.ERP.Shared.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Karasu.ERP.Application.Features.Branches.Commands.UpdateBranch;

public record UpdateBranchCommand(
    Guid Id,
    string Name,
    string Code,
    string? Address,
    string? City,
    string? Phone,
    bool IsActive) : IRequest<Result>;

public class UpdateBranchCommandHandler : IRequestHandler<UpdateBranchCommand, Result>
{
    private readonly IApplicationDbContext _context;
    private readonly ITenantContext _tenantContext;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateBranchCommandHandler(
        IApplicationDbContext context,
        ITenantContext tenantContext,
        IUnitOfWork unitOfWork)
    {
        _context = context;
        _tenantContext = tenantContext;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(UpdateBranchCommand request, CancellationToken cancellationToken)
    {
        var branch = await _context.Branches.FirstOrDefaultAsync(
            b => b.Id == request.Id && b.TenantId == _tenantContext.TenantId && !b.IsDeleted,
            cancellationToken);

        if (branch is null)
            return Result.Failure("Şube bulunamadı.", "BRANCH_NOT_FOUND");

        var code = request.Code.Trim().ToUpperInvariant();
        var codeExists = await _context.Branches.AnyAsync(
            b => b.TenantId == _tenantContext.TenantId &&
                 b.Code == code &&
                 b.Id != request.Id &&
                 !b.IsDeleted,
            cancellationToken);

        if (codeExists)
            return Result.Failure("Bu şube kodu zaten kullanılıyor.", "BRANCH_CODE_EXISTS");

        branch.Name = request.Name.Trim();
        branch.Code = code;
        branch.Address = request.Address?.Trim();
        branch.City = request.City?.Trim();
        branch.Phone = request.Phone?.Trim();
        branch.IsActive = request.IsActive;
        branch.UpdatedAt = DateTime.UtcNow;

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
