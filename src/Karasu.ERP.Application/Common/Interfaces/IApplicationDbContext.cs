using Karasu.ERP.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Karasu.ERP.Application.Common.Interfaces;

public interface IApplicationDbContext
{
    DbSet<Tenant> Tenants { get; }
    DbSet<Branch> Branches { get; }
    DbSet<Product> Products { get; }
    DbSet<ProductVariant> ProductVariants { get; }
    DbSet<Category> Categories { get; }
    DbSet<Brand> Brands { get; }
    DbSet<Unit> Units { get; }
    DbSet<Customer> Customers { get; }
    DbSet<CustomerNote> CustomerNotes { get; }
    DbSet<Order> Orders { get; }
    DbSet<OrderLine> OrderLines { get; }
    DbSet<OrderStatusHistory> OrderStatusHistories { get; }
    DbSet<Quote> Quotes { get; }
    DbSet<QuoteLine> QuoteLines { get; }
    DbSet<Invoice> Invoices { get; }
    DbSet<InvoiceLine> InvoiceLines { get; }
    DbSet<Warehouse> Warehouses { get; }
    DbSet<StockItem> StockItems { get; }
    DbSet<StockMovement> StockMovements { get; }
    DbSet<StockTransfer> StockTransfers { get; }
    DbSet<StockTransferLine> StockTransferLines { get; }
    DbSet<StockCount> StockCounts { get; }
    DbSet<StockCountLine> StockCountLines { get; }
    DbSet<PosSession> PosSessions { get; }
    DbSet<PosTransaction> PosTransactions { get; }
    DbSet<PosReturn> PosReturns { get; }
    DbSet<CashRegister> CashRegisters { get; }
    DbSet<CashTransaction> CashTransactions { get; }
    DbSet<BankAccount> BankAccounts { get; }
    DbSet<BankTransaction> BankTransactions { get; }
    DbSet<ExpenseCategory> ExpenseCategories { get; }
    DbSet<Expense> Expenses { get; }
    DbSet<IncomeCategory> IncomeCategories { get; }
    DbSet<Income> Incomes { get; }
    DbSet<Receivable> Receivables { get; }
    DbSet<Payable> Payables { get; }
    DbSet<FinancePayment> FinancePayments { get; }
    DbSet<AuditLog> AuditLogs { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
    void MarkUnchanged<T>(T entity) where T : class;
}
