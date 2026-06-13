using Karasu.ERP.Application.Common.Interfaces;
using Karasu.ERP.Domain.Entities;
using Karasu.ERP.Domain.Enums;
using Karasu.ERP.Shared.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Karasu.ERP.Application.Features.Finance.Commands.CreateBankAccount;

public class CreateBankAccountCommandHandler : IRequestHandler<CreateBankAccountCommand, Result<Guid>>
{
    private readonly IApplicationDbContext _context;
    private readonly ITenantContext _tenantContext;
    private readonly IUnitOfWork _unitOfWork;

    public CreateBankAccountCommandHandler(
        IApplicationDbContext context,
        ITenantContext tenantContext,
        IUnitOfWork unitOfWork)
    {
        _context = context;
        _tenantContext = tenantContext;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<Guid>> Handle(
        CreateBankAccountCommand request,
        CancellationToken cancellationToken)
    {
        var bankName = request.BankName.Trim();
        var accountName = request.AccountName.Trim();

        var accountExists = await _context.BankAccounts.AnyAsync(
            b => b.TenantId == _tenantContext.TenantId &&
                 b.BankName == bankName &&
                 b.AccountName == accountName &&
                 !b.IsDeleted,
            cancellationToken);

        if (accountExists)
            return Result<Guid>.Failure("Bu banka hesabı zaten mevcut.", "BANK_ACCOUNT_EXISTS");

        var bankAccount = new BankAccount
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantContext.TenantId,
            BankName = bankName,
            AccountName = accountName,
            Iban = request.Iban?.Trim(),
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        await _context.BankAccounts.AddAsync(bankAccount, cancellationToken);

        if (request.OpeningBalance is > 0)
        {
            try
            {
                bankAccount.ApplyTransaction(BankTransactionType.In, request.OpeningBalance.Value);
            }
            catch (Exception ex) when (ex is ArgumentOutOfRangeException or InvalidOperationException)
            {
                return Result<Guid>.Failure(ex.Message, "INVALID_OPENING_BALANCE");
            }

            await _context.BankTransactions.AddAsync(new BankTransaction
            {
                Id = Guid.NewGuid(),
                TenantId = _tenantContext.TenantId,
                BankAccountId = bankAccount.Id,
                Type = BankTransactionType.In,
                Amount = request.OpeningBalance.Value,
                Description = "Açılış bakiyesi",
                CreatedAt = DateTime.UtcNow
            }, cancellationToken);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result<Guid>.Success(bankAccount.Id);
    }
}
