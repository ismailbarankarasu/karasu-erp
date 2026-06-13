using Karasu.ERP.Application.Common.Interfaces;
using Karasu.ERP.Domain.Entities;
using Karasu.ERP.Shared.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Karasu.ERP.Application.Features.Finance.Commands.CreateBankTransaction;

public class CreateBankTransactionCommandHandler : IRequestHandler<CreateBankTransactionCommand, Result<Guid>>
{
    private readonly IApplicationDbContext _context;
    private readonly ITenantContext _tenantContext;
    private readonly IUnitOfWork _unitOfWork;

    public CreateBankTransactionCommandHandler(
        IApplicationDbContext context,
        ITenantContext tenantContext,
        IUnitOfWork unitOfWork)
    {
        _context = context;
        _tenantContext = tenantContext;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<Guid>> Handle(
        CreateBankTransactionCommand request,
        CancellationToken cancellationToken)
    {
        var bankAccount = await _context.BankAccounts
            .FirstOrDefaultAsync(
                b => b.Id == request.BankAccountId &&
                     b.TenantId == _tenantContext.TenantId &&
                     b.IsActive &&
                     !b.IsDeleted,
                cancellationToken);

        if (bankAccount is null)
            return Result<Guid>.Failure("Banka hesabı bulunamadı.", "BANK_ACCOUNT_NOT_FOUND");

        try
        {
            bankAccount.ApplyTransaction(request.Type, request.Amount);
        }
        catch (Exception ex) when (ex is ArgumentOutOfRangeException or InvalidOperationException)
        {
            return Result<Guid>.Failure(ex.Message, "BANK_TRANSACTION_INVALID");
        }

        var transaction = new BankTransaction
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantContext.TenantId,
            BankAccountId = bankAccount.Id,
            Type = request.Type,
            Amount = request.Amount,
            Description = request.Description?.Trim(),
            ReferenceNo = request.ReferenceNo?.Trim(),
            CreatedAt = DateTime.UtcNow
        };

        await _context.BankTransactions.AddAsync(transaction, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<Guid>.Success(transaction.Id);
    }
}
