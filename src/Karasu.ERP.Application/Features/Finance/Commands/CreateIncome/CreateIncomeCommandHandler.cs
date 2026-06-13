using Karasu.ERP.Application.Common.Interfaces;
using Karasu.ERP.Domain.Entities;
using Karasu.ERP.Domain.Enums;
using Karasu.ERP.Shared.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Karasu.ERP.Application.Features.Finance.Commands.CreateIncome;

public class CreateIncomeCommandHandler : IRequestHandler<CreateIncomeCommand, Result<Guid>>
{
    private readonly IApplicationDbContext _context;
    private readonly ITenantContext _tenantContext;
    private readonly IUnitOfWork _unitOfWork;

    public CreateIncomeCommandHandler(
        IApplicationDbContext context,
        ITenantContext tenantContext,
        IUnitOfWork unitOfWork)
    {
        _context = context;
        _tenantContext = tenantContext;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<Guid>> Handle(
        CreateIncomeCommand request,
        CancellationToken cancellationToken)
    {
        if (request.CategoryId.HasValue)
        {
            var categoryExists = await _context.IncomeCategories.AnyAsync(
                c => c.Id == request.CategoryId.Value &&
                     c.TenantId == _tenantContext.TenantId &&
                     !c.IsDeleted,
                cancellationToken);

            if (!categoryExists)
                return Result<Guid>.Failure("Gelir kategorisi bulunamadı.", "INCOME_CATEGORY_NOT_FOUND");
        }

        var income = new Income
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantContext.TenantId,
            CategoryId = request.CategoryId,
            Amount = request.Amount,
            Description = request.Description.Trim(),
            IncomeDate = request.IncomeDate,
            Source = request.Source?.Trim(),
            CashRegisterId = request.CashRegisterId,
            BankAccountId = request.BankAccountId,
            CreatedAt = DateTime.UtcNow
        };

        await _context.Incomes.AddAsync(income, cancellationToken);

        if (request.CashRegisterId.HasValue)
        {
            var cashRegister = await _context.CashRegisters
                .FirstOrDefaultAsync(
                    c => c.Id == request.CashRegisterId.Value &&
                         c.TenantId == _tenantContext.TenantId &&
                         c.IsActive &&
                         !c.IsDeleted,
                    cancellationToken);

            if (cashRegister is null)
                return Result<Guid>.Failure("Kasa bulunamadı.", "CASH_REGISTER_NOT_FOUND");

            try
            {
                cashRegister.ApplyTransaction(CashTransactionType.In, request.Amount);
            }
            catch (Exception ex) when (ex is ArgumentOutOfRangeException or InvalidOperationException)
            {
                return Result<Guid>.Failure(ex.Message, "CASH_TRANSACTION_INVALID");
            }

            await _context.CashTransactions.AddAsync(new CashTransaction
            {
                Id = Guid.NewGuid(),
                TenantId = _tenantContext.TenantId,
                CashRegisterId = cashRegister.Id,
                Type = CashTransactionType.In,
                Amount = request.Amount,
                Description = request.Description.Trim(),
                ReferenceType = FinanceReferenceTypes.Income,
                ReferenceId = income.Id,
                CreatedAt = DateTime.UtcNow
            }, cancellationToken);
        }
        else if (request.BankAccountId.HasValue)
        {
            var bankAccount = await _context.BankAccounts
                .FirstOrDefaultAsync(
                    b => b.Id == request.BankAccountId.Value &&
                         b.TenantId == _tenantContext.TenantId &&
                         b.IsActive &&
                         !b.IsDeleted,
                    cancellationToken);

            if (bankAccount is null)
                return Result<Guid>.Failure("Banka hesabı bulunamadı.", "BANK_ACCOUNT_NOT_FOUND");

            try
            {
                bankAccount.ApplyTransaction(BankTransactionType.In, request.Amount);
            }
            catch (Exception ex) when (ex is ArgumentOutOfRangeException or InvalidOperationException)
            {
                return Result<Guid>.Failure(ex.Message, "BANK_TRANSACTION_INVALID");
            }

            await _context.BankTransactions.AddAsync(new BankTransaction
            {
                Id = Guid.NewGuid(),
                TenantId = _tenantContext.TenantId,
                BankAccountId = bankAccount.Id,
                Type = BankTransactionType.In,
                Amount = request.Amount,
                Description = request.Description.Trim(),
                ReferenceNo = income.Id.ToString(),
                CreatedAt = DateTime.UtcNow
            }, cancellationToken);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result<Guid>.Success(income.Id);
    }
}
