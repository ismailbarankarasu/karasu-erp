using Karasu.ERP.Application.Common.Interfaces;
using Karasu.ERP.Domain.Entities;
using Karasu.ERP.Domain.Enums;
using Karasu.ERP.Shared.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Karasu.ERP.Application.Features.Finance.Commands.CreateFinancePayment;

public class CreateFinancePaymentCommandHandler : IRequestHandler<CreateFinancePaymentCommand, Result<Guid>>
{
    private readonly IApplicationDbContext _context;
    private readonly ITenantContext _tenantContext;
    private readonly IUnitOfWork _unitOfWork;

    public CreateFinancePaymentCommandHandler(
        IApplicationDbContext context,
        ITenantContext tenantContext,
        IUnitOfWork unitOfWork)
    {
        _context = context;
        _tenantContext = tenantContext;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<Guid>> Handle(
        CreateFinancePaymentCommand request,
        CancellationToken cancellationToken)
    {
        if (request.Direction == FinancePaymentDirection.Collection && request.PayableId.HasValue)
            return Result<Guid>.Failure("Tahsilat işleminde borç kaydı kullanılamaz.", "INVALID_PAYMENT_DIRECTION");

        if (request.Direction == FinancePaymentDirection.Disbursement && request.ReceivableId.HasValue)
            return Result<Guid>.Failure("Ödeme işleminde alacak kaydı kullanılamaz.", "INVALID_PAYMENT_DIRECTION");

        Customer? customer = null;
        Receivable? receivable = null;
        Payable? payable = null;

        if (request.ReceivableId.HasValue)
        {
            receivable = await _context.Receivables
                .Include(r => r.Customer)
                .FirstOrDefaultAsync(
                    r => r.Id == request.ReceivableId.Value &&
                         r.TenantId == _tenantContext.TenantId &&
                         !r.IsDeleted,
                    cancellationToken);

            if (receivable is null)
                return Result<Guid>.Failure("Alacak kaydı bulunamadı.", "RECEIVABLE_NOT_FOUND");

            if (receivable.Status is ReceivableStatus.Paid or ReceivableStatus.Cancelled)
                return Result<Guid>.Failure("Bu alacak kaydı için ödeme yapılamaz.", "RECEIVABLE_NOT_PAYABLE");

            if (request.Amount > receivable.RemainingAmount)
                return Result<Guid>.Failure("Ödeme tutarı kalan alacak bakiyesini aşamaz.", "PAYMENT_EXCEEDS_RECEIVABLE");

            customer = receivable.Customer;
        }

        if (request.PayableId.HasValue)
        {
            payable = await _context.Payables
                .FirstOrDefaultAsync(
                    p => p.Id == request.PayableId.Value &&
                         p.TenantId == _tenantContext.TenantId &&
                         !p.IsDeleted,
                    cancellationToken);

            if (payable is null)
                return Result<Guid>.Failure("Borç kaydı bulunamadı.", "PAYABLE_NOT_FOUND");

            if (payable.Status is PayableStatus.Paid or PayableStatus.Cancelled)
                return Result<Guid>.Failure("Bu borç kaydı için ödeme yapılamaz.", "PAYABLE_NOT_PAYABLE");

            if (request.Amount > payable.RemainingAmount)
                return Result<Guid>.Failure("Ödeme tutarı kalan borç bakiyesini aşamaz.", "PAYMENT_EXCEEDS_PAYABLE");
        }

        var payment = new FinancePayment
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantContext.TenantId,
            Direction = request.Direction,
            Amount = request.Amount,
            PaidAt = request.PaidAt,
            ReferenceNo = request.ReferenceNo?.Trim(),
            Note = request.Note?.Trim(),
            ReceivableId = request.ReceivableId,
            PayableId = request.PayableId,
            CashRegisterId = request.CashRegisterId,
            BankAccountId = request.BankAccountId,
            CustomerId = customer?.Id,
            InvoiceId = receivable?.InvoiceId,
            OrderId = receivable?.OrderId,
            CreatedAt = DateTime.UtcNow
        };

        if (request.CashRegisterId.HasValue)
            payment.Method = PaymentMethod.Cash;
        else if (request.BankAccountId.HasValue)
            payment.Method = PaymentMethod.BankTransfer;
        else
            payment.Method = PaymentMethod.Credit;

        if (receivable is not null)
        {
            try
            {
                receivable.ApplyPayment(request.Amount);
            }
            catch (Exception ex) when (ex is ArgumentOutOfRangeException or InvalidOperationException)
            {
                return Result<Guid>.Failure(ex.Message, "RECEIVABLE_PAYMENT_INVALID");
            }

            if (customer is not null)
                customer.Balance -= request.Amount;
        }

        if (payable is not null)
        {
            try
            {
                payable.ApplyPayment(request.Amount);
            }
            catch (Exception ex) when (ex is ArgumentOutOfRangeException or InvalidOperationException)
            {
                return Result<Guid>.Failure(ex.Message, "PAYABLE_PAYMENT_INVALID");
            }
        }

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

            var transactionType = request.Direction == FinancePaymentDirection.Collection
                ? CashTransactionType.In
                : CashTransactionType.Out;

            try
            {
                cashRegister.ApplyTransaction(transactionType, request.Amount);
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
                Type = transactionType,
                Amount = request.Amount,
                Description = request.Note?.Trim() ?? "Finans ödemesi",
                ReferenceType = FinanceReferenceTypes.Payment,
                ReferenceId = payment.Id,
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

            var transactionType = request.Direction == FinancePaymentDirection.Collection
                ? BankTransactionType.In
                : BankTransactionType.Out;

            try
            {
                bankAccount.ApplyTransaction(transactionType, request.Amount);
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
                Type = transactionType,
                Amount = request.Amount,
                Description = request.Note?.Trim() ?? "Finans ödemesi",
                ReferenceNo = request.ReferenceNo?.Trim() ?? payment.Id.ToString(),
                CreatedAt = DateTime.UtcNow
            }, cancellationToken);
        }

        await _context.FinancePayments.AddAsync(payment, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<Guid>.Success(payment.Id);
    }
}
