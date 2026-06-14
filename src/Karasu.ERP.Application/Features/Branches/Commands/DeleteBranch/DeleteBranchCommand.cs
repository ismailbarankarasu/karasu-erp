using Karasu.ERP.Application.Common.Interfaces;
using Karasu.ERP.Shared.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Karasu.ERP.Application.Features.Branches.Commands.DeleteBranch;

public record DeleteBranchCommand(Guid Id) : IRequest<Result>;

public class DeleteBranchCommandHandler : IRequestHandler<DeleteBranchCommand, Result>
{
    private readonly IApplicationDbContext _context;
    private readonly ITenantContext _tenantContext;
    private readonly IUnitOfWork _unitOfWork;

    public DeleteBranchCommandHandler(
        IApplicationDbContext context,
        ITenantContext tenantContext,
        IUnitOfWork unitOfWork)
    {
        _context = context;
        _tenantContext = tenantContext;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(DeleteBranchCommand request, CancellationToken cancellationToken)
    {
        var branch = await _context.Branches.FirstOrDefaultAsync(
            b => b.Id == request.Id && b.TenantId == _tenantContext.TenantId && !b.IsDeleted,
            cancellationToken);

        if (branch is null)
            return Result.Failure("Şube bulunamadı.", "BRANCH_NOT_FOUND");

        if (branch.Code == "MAIN")
            return Result.Failure("Ana şube silinemez.", "BRANCH_MAIN_PROTECTED");

        var hasOrders = await _context.Orders.AnyAsync(
            o => o.BranchId == request.Id && !o.IsDeleted,
            cancellationToken);

        if (hasOrders)
            return Result.Failure("Siparişi olan şube silinemez.", "BRANCH_HAS_ORDERS");

        branch.IsDeleted = true;
        branch.UpdatedAt = DateTime.UtcNow;
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
