using Karasu.ERP.Application.Common.Interfaces;
using Karasu.ERP.Domain.Entities;
using Karasu.ERP.Domain.Enums;
using Karasu.ERP.Shared.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Karasu.ERP.Application.Features.Finance.Commands.CreateCashRegister;

public class CreateCashRegisterCommandHandler : IRequestHandler<CreateCashRegisterCommand, Result<Guid>>
{
    private readonly IApplicationDbContext _context;
    private readonly ITenantContext _tenantContext;
    private readonly IUnitOfWork _unitOfWork;

    public CreateCashRegisterCommandHandler(
        IApplicationDbContext context,
        ITenantContext tenantContext,
        IUnitOfWork unitOfWork)
    {
        _context = context;
        _tenantContext = tenantContext;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<Guid>> Handle(
        CreateCashRegisterCommand request,
        CancellationToken cancellationToken)
    {
        var branchExists = await _context.Branches.AnyAsync(
            b => b.Id == request.BranchId &&
                 b.TenantId == _tenantContext.TenantId &&
                 !b.IsDeleted,
            cancellationToken);

        if (!branchExists)
            return Result<Guid>.Failure("Şube bulunamadı.", "BRANCH_NOT_FOUND");

        var name = request.Name.Trim();
        var nameExists = await _context.CashRegisters.AnyAsync(
            c => c.TenantId == _tenantContext.TenantId &&
                 c.BranchId == request.BranchId &&
                 c.Name == name &&
                 !c.IsDeleted,
            cancellationToken);

        if (nameExists)
            return Result<Guid>.Failure("Bu şubede aynı isimde kasa zaten mevcut.", "CASH_REGISTER_NAME_EXISTS");

        var cashRegister = new CashRegister
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantContext.TenantId,
            BranchId = request.BranchId,
            Name = name,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        await _context.CashRegisters.AddAsync(cashRegister, cancellationToken);

        if (request.OpeningBalance is > 0)
        {
            try
            {
                cashRegister.ApplyTransaction(CashTransactionType.In, request.OpeningBalance.Value);
            }
            catch (Exception ex) when (ex is ArgumentOutOfRangeException or InvalidOperationException)
            {
                return Result<Guid>.Failure(ex.Message, "INVALID_OPENING_BALANCE");
            }

            await _context.CashTransactions.AddAsync(new CashTransaction
            {
                Id = Guid.NewGuid(),
                TenantId = _tenantContext.TenantId,
                CashRegisterId = cashRegister.Id,
                Type = CashTransactionType.In,
                Amount = request.OpeningBalance.Value,
                Description = "Açılış bakiyesi",
                CreatedAt = DateTime.UtcNow
            }, cancellationToken);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result<Guid>.Success(cashRegister.Id);
    }
}
