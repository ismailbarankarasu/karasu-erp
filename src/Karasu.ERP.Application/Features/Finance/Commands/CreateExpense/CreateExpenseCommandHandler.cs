using Karasu.ERP.Application.Common.Interfaces;
using Karasu.ERP.Domain.Entities;
using Karasu.ERP.Domain.Enums;
using Karasu.ERP.Shared.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Karasu.ERP.Application.Features.Finance.Commands.CreateExpense;

public class CreateExpenseCommandHandler : IRequestHandler<CreateExpenseCommand, Result<Guid>>
{
    private readonly IApplicationDbContext _context;
    private readonly ITenantContext _tenantContext;
    private readonly IUnitOfWork _unitOfWork;

    public CreateExpenseCommandHandler(
        IApplicationDbContext context,
        ITenantContext tenantContext,
        IUnitOfWork unitOfWork)
    {
        _context = context;
        _tenantContext = tenantContext;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<Guid>> Handle(
        CreateExpenseCommand request,
        CancellationToken cancellationToken)
    {
        if (request.CategoryId.HasValue)
        {
            var categoryExists = await _context.ExpenseCategories.AnyAsync(
                c => c.Id == request.CategoryId.Value &&
                     c.TenantId == _tenantContext.TenantId &&
                     !c.IsDeleted,
                cancellationToken);

            if (!categoryExists)
                return Result<Guid>.Failure("Gider kategorisi bulunamadı.", "EXPENSE_CATEGORY_NOT_FOUND");
        }

        var expense = new Expense
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantContext.TenantId,
            CategoryId = request.CategoryId,
            Amount = request.Amount,
            Description = request.Description.Trim(),
            ExpenseDate = request.ExpenseDate,
            PaymentMethod = request.PaymentMethod,
            CashRegisterId = request.CashRegisterId,
            BankAccountId = request.BankAccountId,
            CreatedAt = DateTime.UtcNow
        };

        await _context.Expenses.AddAsync(expense, cancellationToken);

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
                cashRegister.ApplyTransaction(CashTransactionType.Out, request.Amount);
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
                Type = CashTransactionType.Out,
                Amount = request.Amount,
                Description = request.Description.Trim(),
                ReferenceType = FinanceReferenceTypes.Expense,
                ReferenceId = expense.Id,
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
                bankAccount.ApplyTransaction(BankTransactionType.Out, request.Amount);
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
                Type = BankTransactionType.Out,
                Amount = request.Amount,
                Description = request.Description.Trim(),
                ReferenceNo = expense.Id.ToString(),
                CreatedAt = DateTime.UtcNow
            }, cancellationToken);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result<Guid>.Success(expense.Id);
    }
}
