using Karasu.ERP.Application.Common.Interfaces;
using Karasu.ERP.Domain.Entities;
using Karasu.ERP.Shared.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Karasu.ERP.Application.Features.Finance.Commands.CreateCashTransaction;

public class CreateCashTransactionCommandHandler : IRequestHandler<CreateCashTransactionCommand, Result<Guid>>
{
    private readonly IApplicationDbContext _context;
    private readonly ITenantContext _tenantContext;
    private readonly IUnitOfWork _unitOfWork;

    public CreateCashTransactionCommandHandler(
        IApplicationDbContext context,
        ITenantContext tenantContext,
        IUnitOfWork unitOfWork)
    {
        _context = context;
        _tenantContext = tenantContext;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<Guid>> Handle(
        CreateCashTransactionCommand request,
        CancellationToken cancellationToken)
    {
        var cashRegister = await _context.CashRegisters
            .FirstOrDefaultAsync(
                c => c.Id == request.CashRegisterId &&
                     c.TenantId == _tenantContext.TenantId &&
                     c.IsActive &&
                     !c.IsDeleted,
                cancellationToken);

        if (cashRegister is null)
            return Result<Guid>.Failure("Kasa bulunamadı.", "CASH_REGISTER_NOT_FOUND");

        try
        {
            cashRegister.ApplyTransaction(request.Type, request.Amount);
        }
        catch (Exception ex) when (ex is ArgumentOutOfRangeException or InvalidOperationException)
        {
            return Result<Guid>.Failure(ex.Message, "CASH_TRANSACTION_INVALID");
        }

        var transaction = new CashTransaction
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantContext.TenantId,
            CashRegisterId = cashRegister.Id,
            Type = request.Type,
            Amount = request.Amount,
            Description = request.Description?.Trim(),
            CreatedAt = DateTime.UtcNow
        };

        await _context.CashTransactions.AddAsync(transaction, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<Guid>.Success(transaction.Id);
    }
}
