using Karasu.ERP.Domain.Common;
using Karasu.ERP.Domain.Enums;

namespace Karasu.ERP.Domain.Entities;

public class CashRegister : TenantEntity
{
    public Guid BranchId { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal CurrentBalance { get; private set; }
    public bool IsActive { get; set; } = true;

    public Branch Branch { get; set; } = null!;
    public ICollection<CashTransaction> Transactions { get; set; } = new List<CashTransaction>();

    public void ApplyTransaction(CashTransactionType type, decimal amount)
    {
        if (amount <= 0)
            throw new ArgumentOutOfRangeException(nameof(amount), "Tutar sıfırdan büyük olmalıdır.");

        if (type == CashTransactionType.Out && CurrentBalance < amount)
            throw new InvalidOperationException("Kasa bakiyesi yetersiz.");

        CurrentBalance = type == CashTransactionType.In
            ? CurrentBalance + amount
            : CurrentBalance - amount;
    }
}

public class CashTransaction : TenantEntity
{
    public Guid CashRegisterId { get; set; }
    public CashTransactionType Type { get; set; }
    public decimal Amount { get; set; }
    public string? Description { get; set; }
    public string? ReferenceType { get; set; }
    public Guid? ReferenceId { get; set; }

    public CashRegister CashRegister { get; set; } = null!;
}

public class BankAccount : TenantEntity
{
    public string BankName { get; set; } = string.Empty;
    public string AccountName { get; set; } = string.Empty;
    public string? Iban { get; set; }
    public decimal CurrentBalance { get; private set; }
    public bool IsActive { get; set; } = true;

    public ICollection<BankTransaction> Transactions { get; set; } = new List<BankTransaction>();

    public void ApplyTransaction(BankTransactionType type, decimal amount)
    {
        if (amount <= 0)
            throw new ArgumentOutOfRangeException(nameof(amount), "Tutar sıfırdan büyük olmalıdır.");

        if (type == BankTransactionType.Out && CurrentBalance < amount)
            throw new InvalidOperationException("Banka bakiyesi yetersiz.");

        CurrentBalance = type == BankTransactionType.In
            ? CurrentBalance + amount
            : CurrentBalance - amount;
    }
}

public class BankTransaction : TenantEntity
{
    public Guid BankAccountId { get; set; }
    public BankTransactionType Type { get; set; }
    public decimal Amount { get; set; }
    public string? Description { get; set; }
    public string? ReferenceNo { get; set; }

    public BankAccount BankAccount { get; set; } = null!;
}

public class ExpenseCategory : TenantEntity
{
    public string Name { get; set; } = string.Empty;
    public Guid? ParentId { get; set; }

    public ExpenseCategory? Parent { get; set; }
    public ICollection<ExpenseCategory> Children { get; set; } = new List<ExpenseCategory>();
}

public class Expense : TenantEntity
{
    public Guid? CategoryId { get; set; }
    public decimal Amount { get; set; }
    public string Description { get; set; } = string.Empty;
    public DateTime ExpenseDate { get; set; }
    public PaymentMethod PaymentMethod { get; set; }
    public Guid? CashRegisterId { get; set; }
    public Guid? BankAccountId { get; set; }

    public ExpenseCategory? Category { get; set; }
}

public class IncomeCategory : TenantEntity
{
    public string Name { get; set; } = string.Empty;
    public Guid? ParentId { get; set; }

    public IncomeCategory? Parent { get; set; }
    public ICollection<IncomeCategory> Children { get; set; } = new List<IncomeCategory>();
}

public class Income : TenantEntity
{
    public Guid? CategoryId { get; set; }
    public decimal Amount { get; set; }
    public string Description { get; set; } = string.Empty;
    public DateTime IncomeDate { get; set; }
    public string? Source { get; set; }
    public Guid? CashRegisterId { get; set; }
    public Guid? BankAccountId { get; set; }

    public IncomeCategory? Category { get; set; }
}

public class Receivable : TenantEntity
{
    public Guid CustomerId { get; set; }
    public Guid? InvoiceId { get; set; }
    public Guid? OrderId { get; set; }
    public decimal Amount { get; set; }
    public decimal PaidAmount { get; private set; }
    public DateTime DueDate { get; set; }
    public ReceivableStatus Status { get; set; } = ReceivableStatus.Open;
    public string? Description { get; set; }

    public Customer Customer { get; set; } = null!;
    public decimal RemainingAmount => Amount - PaidAmount;

    public void ApplyPayment(decimal amount)
    {
        if (amount <= 0)
            throw new ArgumentOutOfRangeException(nameof(amount));

        if (PaidAmount + amount > Amount)
            throw new InvalidOperationException("Ödeme tutarı alacak bakiyesini aşamaz.");

        PaidAmount += amount;
        Status = PaidAmount >= Amount
            ? ReceivableStatus.Paid
            : ReceivableStatus.PartiallyPaid;
    }
}

public class Payable : TenantEntity
{
    public string CreditorName { get; set; } = string.Empty;
    public Guid? SupplierId { get; set; }
    public decimal Amount { get; set; }
    public decimal PaidAmount { get; private set; }
    public DateTime DueDate { get; set; }
    public PayableStatus Status { get; set; } = PayableStatus.Open;
    public string? Description { get; set; }

    public decimal RemainingAmount => Amount - PaidAmount;

    public void ApplyPayment(decimal amount)
    {
        if (amount <= 0)
            throw new ArgumentOutOfRangeException(nameof(amount));

        if (PaidAmount + amount > Amount)
            throw new InvalidOperationException("Ödeme tutarı borç bakiyesini aşamaz.");

        PaidAmount += amount;
        Status = PaidAmount >= Amount
            ? PayableStatus.Paid
            : PayableStatus.PartiallyPaid;
    }
}

public class FinancePayment : TenantEntity
{
    public FinancePaymentDirection Direction { get; set; }
    public PaymentMethod Method { get; set; }
    public decimal Amount { get; set; }
    public DateTime PaidAt { get; set; }
    public string? ReferenceNo { get; set; }
    public string? Note { get; set; }
    public Guid? CustomerId { get; set; }
    public Guid? ReceivableId { get; set; }
    public Guid? PayableId { get; set; }
    public Guid? InvoiceId { get; set; }
    public Guid? OrderId { get; set; }
    public Guid? CashRegisterId { get; set; }
    public Guid? BankAccountId { get; set; }

    public Customer? Customer { get; set; }
    public Receivable? Receivable { get; set; }
    public Payable? Payable { get; set; }
}

public static class FinanceReferenceTypes
{
    public const string Payment = "FinancePayment";
    public const string Expense = "Expense";
    public const string Income = "Income";
    public const string Invoice = "Invoice";
    public const string Order = "Order";
}
