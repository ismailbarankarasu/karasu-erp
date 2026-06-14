using Karasu.ERP.Domain.Entities;
using Karasu.ERP.Identity.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Karasu.ERP.Persistence.Configurations;

public class TenantConfiguration : IEntityTypeConfiguration<Tenant>
{
    public void Configure(EntityTypeBuilder<Tenant> builder)
    {
        builder.ToTable("Tenants");
        builder.HasKey(t => t.Id);
        builder.Property(t => t.Name).HasMaxLength(200).IsRequired();
        builder.Property(t => t.Slug).HasMaxLength(100).IsRequired();
        builder.HasIndex(t => t.Slug).IsUnique();
        builder.Property(t => t.SettingsJson).HasColumnType("nvarchar(max)");
    }
}

public class BranchConfiguration : IEntityTypeConfiguration<Branch>
{
    public void Configure(EntityTypeBuilder<Branch> builder)
    {
        builder.ToTable("Branches");
        builder.HasKey(b => b.Id);
        builder.Property(b => b.Name).HasMaxLength(200).IsRequired();
        builder.Property(b => b.Code).HasMaxLength(50).IsRequired();
        builder.HasIndex(b => new { b.TenantId, b.Code }).IsUnique();
        builder.HasOne(b => b.Tenant).WithMany(t => t.Branches).HasForeignKey(b => b.TenantId);
    }
}

public class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.ToTable("Products");
        builder.HasKey(p => p.Id);
        builder.Property(p => p.Sku).HasMaxLength(50).IsRequired();
        builder.Property(p => p.Barcode).HasMaxLength(100);
        builder.Property(p => p.Name).HasMaxLength(300).IsRequired();
        builder.Property(p => p.PurchasePrice).HasPrecision(18, 4);
        builder.Property(p => p.SalePrice).HasPrecision(18, 4);
        builder.Property(p => p.TaxRate).HasPrecision(5, 2);
        builder.HasIndex(p => new { p.TenantId, p.Sku });
        builder.HasIndex(p => new { p.TenantId, p.Barcode });
    }
}

public class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> builder)
    {
        builder.ToTable("Orders");
        builder.HasKey(o => o.Id);
        builder.Property(o => o.OrderNumber).HasMaxLength(50).IsRequired();
        builder.Property(o => o.SubTotal).HasPrecision(18, 4);
        builder.Property(o => o.TaxTotal).HasPrecision(18, 4);
        builder.Property(o => o.DiscountTotal).HasPrecision(18, 4);
        builder.Property(o => o.GrandTotal).HasPrecision(18, 4);
        builder.HasIndex(o => new { o.TenantId, o.OrderNumber }).IsUnique();
        builder.HasIndex(o => new { o.TenantId, o.Status, o.CreatedAt });
        builder.HasMany(o => o.Lines).WithOne(l => l.Order).HasForeignKey(l => l.OrderId).OnDelete(DeleteBehavior.Cascade);
    }
}

public class QuoteConfiguration : IEntityTypeConfiguration<Quote>
{
    public void Configure(EntityTypeBuilder<Quote> builder)
    {
        builder.ToTable("Quotes");
        builder.HasKey(q => q.Id);
        builder.Property(q => q.QuoteNumber).HasMaxLength(50).IsRequired();
        builder.Property(q => q.SubTotal).HasPrecision(18, 4);
        builder.Property(q => q.TaxTotal).HasPrecision(18, 4);
        builder.Property(q => q.DiscountTotal).HasPrecision(18, 4);
        builder.Property(q => q.GrandTotal).HasPrecision(18, 4);
        builder.HasIndex(q => new { q.TenantId, q.QuoteNumber }).IsUnique();
        builder.HasIndex(q => new { q.TenantId, q.Status, q.CreatedAt });
        builder.HasMany(q => q.Lines).WithOne(l => l.Quote).HasForeignKey(l => l.QuoteId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(q => q.Branch).WithMany().HasForeignKey(q => q.BranchId).OnDelete(DeleteBehavior.NoAction);
        builder.HasOne(q => q.Customer).WithMany().HasForeignKey(q => q.CustomerId).OnDelete(DeleteBehavior.NoAction);
        builder.HasOne(q => q.ConvertedOrder).WithMany().HasForeignKey(q => q.ConvertedOrderId).OnDelete(DeleteBehavior.NoAction);
    }
}

public class InvoiceConfiguration : IEntityTypeConfiguration<Invoice>
{
    public void Configure(EntityTypeBuilder<Invoice> builder)
    {
        builder.ToTable("Invoices");
        builder.HasKey(i => i.Id);
        builder.Property(i => i.InvoiceNumber).HasMaxLength(50).IsRequired();
        builder.Property(i => i.SubTotal).HasPrecision(18, 4);
        builder.Property(i => i.TaxTotal).HasPrecision(18, 4);
        builder.Property(i => i.GrandTotal).HasPrecision(18, 4);
        builder.HasIndex(i => new { i.TenantId, i.InvoiceNumber }).IsUnique();
        builder.HasIndex(i => new { i.TenantId, i.OrderId });
        builder.HasMany(i => i.Lines).WithOne(l => l.Invoice).HasForeignKey(l => l.InvoiceId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(i => i.Order).WithMany().HasForeignKey(i => i.OrderId).OnDelete(DeleteBehavior.NoAction);
        builder.HasOne(i => i.Customer).WithMany().HasForeignKey(i => i.CustomerId).OnDelete(DeleteBehavior.NoAction);
    }
}

public class QuoteLineConfiguration : IEntityTypeConfiguration<QuoteLine>
{
    public void Configure(EntityTypeBuilder<QuoteLine> builder)
    {
        builder.ToTable("QuoteLines");
        builder.HasKey(l => l.Id);
        builder.Property(l => l.Quantity).HasPrecision(18, 4);
        builder.Property(l => l.UnitPrice).HasPrecision(18, 4);
        builder.Property(l => l.TaxRate).HasPrecision(5, 2);
        builder.Property(l => l.Discount).HasPrecision(18, 4);
        builder.Property(l => l.LineTotal).HasPrecision(18, 4);
    }
}

public class InvoiceLineConfiguration : IEntityTypeConfiguration<InvoiceLine>
{
    public void Configure(EntityTypeBuilder<InvoiceLine> builder)
    {
        builder.ToTable("InvoiceLines");
        builder.HasKey(l => l.Id);
        builder.Property(l => l.Description).HasMaxLength(500).IsRequired();
        builder.Property(l => l.Quantity).HasPrecision(18, 4);
        builder.Property(l => l.UnitPrice).HasPrecision(18, 4);
        builder.Property(l => l.TaxRate).HasPrecision(5, 2);
        builder.Property(l => l.LineTotal).HasPrecision(18, 4);
    }
}

public class CustomerConfiguration : IEntityTypeConfiguration<Customer>
{
    public void Configure(EntityTypeBuilder<Customer> builder)
    {
        builder.ToTable("Customers");
        builder.HasKey(c => c.Id);
        builder.Property(c => c.FullName).HasMaxLength(200).IsRequired();
        builder.Property(c => c.Balance).HasPrecision(18, 4);
        builder.Property(c => c.CreditLimit).HasPrecision(18, 4);
        builder.HasIndex(c => new { c.TenantId, c.TaxNumber });
    }
}

public class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> builder)
    {
        builder.ToTable("AuditLogs");
        builder.HasKey(a => a.Id);
        builder.Property(a => a.EntityType).HasMaxLength(100).IsRequired();
        builder.Property(a => a.Action).HasMaxLength(50).IsRequired();
        builder.Property(a => a.OldValues).HasColumnType("nvarchar(max)");
        builder.Property(a => a.NewValues).HasColumnType("nvarchar(max)");
        builder.HasIndex(a => new { a.TenantId, a.EntityType, a.EntityId });
    }
}

public class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        builder.ToTable("RefreshTokens");
        builder.HasKey(r => r.Id);
        builder.Property(r => r.Token).HasMaxLength(500).IsRequired();
        builder.HasIndex(r => r.Token);
        builder.HasOne(r => r.User).WithMany().HasForeignKey(r => r.UserId).OnDelete(DeleteBehavior.Cascade);
    }
}

public class PermissionConfiguration : IEntityTypeConfiguration<Permission>
{
    public void Configure(EntityTypeBuilder<Permission> builder)
    {
        builder.ToTable("Permissions");
        builder.HasKey(p => p.Id);
        builder.Property(p => p.Module).HasMaxLength(50).IsRequired();
        builder.Property(p => p.Entity).HasMaxLength(50).IsRequired();
        builder.Property(p => p.Action).HasMaxLength(50).IsRequired();
        builder.HasIndex(p => new { p.Module, p.Entity, p.Action }).IsUnique();
    }
}

public class RolePermissionConfiguration : IEntityTypeConfiguration<RolePermission>
{
    public void Configure(EntityTypeBuilder<RolePermission> builder)
    {
        builder.ToTable("RolePermissions");
        builder.HasKey(rp => new { rp.RoleId, rp.PermissionId });
        builder.HasOne(rp => rp.Role).WithMany().HasForeignKey(rp => rp.RoleId);
        builder.HasOne(rp => rp.Permission).WithMany(p => p.RolePermissions).HasForeignKey(rp => rp.PermissionId);
    }
}

public class ApplicationUserConfiguration : IEntityTypeConfiguration<ApplicationUser>
{
    public void Configure(EntityTypeBuilder<ApplicationUser> builder)
    {
        builder.Property(u => u.FullName).HasMaxLength(200).IsRequired();
        builder.HasIndex(u => new { u.TenantId, u.Email });
    }
}

public class ApplicationRoleConfiguration : IEntityTypeConfiguration<ApplicationRole>
{
    public void Configure(EntityTypeBuilder<ApplicationRole> builder)
    {
        builder.Property(r => r.Description).HasMaxLength(500);
        builder.HasIndex(r => new { r.TenantId, r.Name });
    }
}

public class WarehouseConfiguration : IEntityTypeConfiguration<Warehouse>
{
    public void Configure(EntityTypeBuilder<Warehouse> builder)
    {
        builder.ToTable("Warehouses");
        builder.HasKey(w => w.Id);
        builder.Property(w => w.Name).HasMaxLength(200).IsRequired();
        builder.Property(w => w.Code).HasMaxLength(50).IsRequired();
        builder.HasIndex(w => new { w.TenantId, w.Code }).IsUnique();
        builder.HasOne(w => w.Branch).WithMany().HasForeignKey(w => w.BranchId);
    }
}

public class StockItemConfiguration : IEntityTypeConfiguration<StockItem>
{
    public void Configure(EntityTypeBuilder<StockItem> builder)
    {
        builder.ToTable("StockItems");
        builder.HasKey(s => s.Id);
        builder.Property(s => s.Quantity).HasPrecision(18, 4);
        builder.Property(s => s.ReservedQuantity).HasPrecision(18, 4);
        builder.Property(s => s.MinStock).HasPrecision(18, 4);
        builder.HasIndex(s => new { s.TenantId, s.WarehouseId, s.ProductVariantId }).IsUnique();
        builder.HasOne(s => s.Warehouse).WithMany(w => w.StockItems).HasForeignKey(s => s.WarehouseId);
        builder.HasOne(s => s.ProductVariant).WithMany().HasForeignKey(s => s.ProductVariantId);
    }
}

public class StockMovementConfiguration : IEntityTypeConfiguration<StockMovement>
{
    public void Configure(EntityTypeBuilder<StockMovement> builder)
    {
        builder.ToTable("StockMovements");
        builder.HasKey(m => m.Id);
        builder.Property(m => m.Quantity).HasPrecision(18, 4);
        builder.Property(m => m.ReferenceType).HasMaxLength(50);
        builder.Property(m => m.Note).HasMaxLength(500);
        builder.HasIndex(m => new { m.TenantId, m.StockItemId, m.CreatedAt });
        builder.HasOne(m => m.StockItem).WithMany(s => s.Movements).HasForeignKey(m => m.StockItemId);
    }
}

public class StockTransferConfiguration : IEntityTypeConfiguration<StockTransfer>
{
    public void Configure(EntityTypeBuilder<StockTransfer> builder)
    {
        builder.ToTable("StockTransfers");
        builder.HasKey(t => t.Id);
        builder.Property(t => t.Note).HasMaxLength(500);
        builder.HasIndex(t => new { t.TenantId, t.Status, t.CreatedAt });
        builder.HasOne(t => t.FromWarehouse).WithMany().HasForeignKey(t => t.FromWarehouseId)
            .OnDelete(DeleteBehavior.NoAction);
        builder.HasOne(t => t.ToWarehouse).WithMany().HasForeignKey(t => t.ToWarehouseId)
            .OnDelete(DeleteBehavior.NoAction);
    }
}

public class StockTransferLineConfiguration : IEntityTypeConfiguration<StockTransferLine>
{
    public void Configure(EntityTypeBuilder<StockTransferLine> builder)
    {
        builder.ToTable("StockTransferLines");
        builder.HasKey(l => l.Id);
        builder.Property(l => l.Quantity).HasPrecision(18, 4);
        builder.HasIndex(l => new { l.TenantId, l.TransferId, l.ProductVariantId });
        builder.HasOne(l => l.Transfer).WithMany(t => t.Lines).HasForeignKey(l => l.TransferId)
            .OnDelete(DeleteBehavior.NoAction);
        builder.HasOne(l => l.ProductVariant).WithMany().HasForeignKey(l => l.ProductVariantId)
            .OnDelete(DeleteBehavior.NoAction);
    }
}

public class StockCountConfiguration : IEntityTypeConfiguration<StockCount>
{
    public void Configure(EntityTypeBuilder<StockCount> builder)
    {
        builder.ToTable("StockCounts");
        builder.HasKey(c => c.Id);
        builder.Property(c => c.Note).HasMaxLength(500);
        builder.HasIndex(c => new { c.TenantId, c.WarehouseId, c.Status });
        builder.HasOne(c => c.Warehouse).WithMany().HasForeignKey(c => c.WarehouseId)
            .OnDelete(DeleteBehavior.NoAction);
    }
}

public class StockCountLineConfiguration : IEntityTypeConfiguration<StockCountLine>
{
    public void Configure(EntityTypeBuilder<StockCountLine> builder)
    {
        builder.ToTable("StockCountLines");
        builder.HasKey(l => l.Id);
        builder.Property(l => l.SystemQty).HasPrecision(18, 4);
        builder.Property(l => l.CountedQty).HasPrecision(18, 4);
        builder.HasIndex(l => new { l.TenantId, l.CountId, l.ProductVariantId });
        builder.HasOne(l => l.Count).WithMany(c => c.Lines).HasForeignKey(l => l.CountId)
            .OnDelete(DeleteBehavior.NoAction);
        builder.HasOne(l => l.ProductVariant).WithMany().HasForeignKey(l => l.ProductVariantId)
            .OnDelete(DeleteBehavior.NoAction);
    }
}

public class PosSessionConfiguration : IEntityTypeConfiguration<PosSession>
{
    public void Configure(EntityTypeBuilder<PosSession> builder)
    {
        builder.ToTable("PosSessions");
        builder.HasKey(s => s.Id);
        builder.Property(s => s.OpeningBalance).HasPrecision(18, 4);
        builder.Property(s => s.ClosingBalance).HasPrecision(18, 4);
        builder.HasIndex(s => new { s.TenantId, s.CashierId, s.Status });
        builder.HasIndex(s => new { s.TenantId, s.BranchId, s.OpenedAt });
        builder.HasOne(s => s.Branch).WithMany().HasForeignKey(s => s.BranchId)
            .OnDelete(DeleteBehavior.NoAction);
    }
}

public class PosTransactionConfiguration : IEntityTypeConfiguration<PosTransaction>
{
    public void Configure(EntityTypeBuilder<PosTransaction> builder)
    {
        builder.ToTable("PosTransactions");
        builder.HasKey(t => t.Id);
        builder.Property(t => t.Amount).HasPrecision(18, 4);
        builder.Property(t => t.ChangeAmount).HasPrecision(18, 4);
        builder.HasIndex(t => new { t.TenantId, t.SessionId, t.CreatedAt });
        builder.HasOne(t => t.Session).WithMany(s => s.Transactions).HasForeignKey(t => t.SessionId)
            .OnDelete(DeleteBehavior.NoAction);
        builder.HasOne(t => t.Order).WithMany().HasForeignKey(t => t.OrderId)
            .OnDelete(DeleteBehavior.NoAction);
    }
}

public class PosReturnConfiguration : IEntityTypeConfiguration<PosReturn>
{
    public void Configure(EntityTypeBuilder<PosReturn> builder)
    {
        builder.ToTable("PosReturns");
        builder.HasKey(r => r.Id);
        builder.Property(r => r.Reason).HasMaxLength(500);
        builder.Property(r => r.RefundAmount).HasPrecision(18, 4);
        builder.HasIndex(r => new { r.TenantId, r.SessionId, r.CreatedAt });
        builder.HasIndex(r => new { r.TenantId, r.OriginalOrderId });
        builder.HasOne(r => r.Session).WithMany().HasForeignKey(r => r.SessionId)
            .OnDelete(DeleteBehavior.NoAction);
        builder.HasOne(r => r.OriginalOrder).WithMany().HasForeignKey(r => r.OriginalOrderId)
            .OnDelete(DeleteBehavior.NoAction);
    }
}

public class CashRegisterConfiguration : IEntityTypeConfiguration<CashRegister>
{
    public void Configure(EntityTypeBuilder<CashRegister> builder)
    {
        builder.ToTable("CashRegisters");
        builder.HasKey(c => c.Id);
        builder.Property(c => c.Name).HasMaxLength(100).IsRequired();
        builder.Property(c => c.CurrentBalance).HasPrecision(18, 4);
        builder.HasIndex(c => new { c.TenantId, c.BranchId, c.Name });
        builder.HasOne(c => c.Branch).WithMany().HasForeignKey(c => c.BranchId).OnDelete(DeleteBehavior.NoAction);
    }
}

public class CashTransactionConfiguration : IEntityTypeConfiguration<CashTransaction>
{
    public void Configure(EntityTypeBuilder<CashTransaction> builder)
    {
        builder.ToTable("CashTransactions");
        builder.HasKey(t => t.Id);
        builder.Property(t => t.Amount).HasPrecision(18, 4);
        builder.Property(t => t.Description).HasMaxLength(500);
        builder.HasIndex(t => new { t.TenantId, t.CashRegisterId, t.CreatedAt });
        builder.HasOne(t => t.CashRegister).WithMany(c => c.Transactions).HasForeignKey(t => t.CashRegisterId)
            .OnDelete(DeleteBehavior.NoAction);
    }
}

public class BankAccountConfiguration : IEntityTypeConfiguration<BankAccount>
{
    public void Configure(EntityTypeBuilder<BankAccount> builder)
    {
        builder.ToTable("BankAccounts");
        builder.HasKey(b => b.Id);
        builder.Property(b => b.BankName).HasMaxLength(100).IsRequired();
        builder.Property(b => b.AccountName).HasMaxLength(100).IsRequired();
        builder.Property(b => b.Iban).HasMaxLength(34);
        builder.Property(b => b.CurrentBalance).HasPrecision(18, 4);
        builder.HasIndex(b => new { b.TenantId, b.BankName, b.AccountName });
    }
}

public class BankTransactionConfiguration : IEntityTypeConfiguration<BankTransaction>
{
    public void Configure(EntityTypeBuilder<BankTransaction> builder)
    {
        builder.ToTable("BankTransactions");
        builder.HasKey(t => t.Id);
        builder.Property(t => t.Amount).HasPrecision(18, 4);
        builder.Property(t => t.Description).HasMaxLength(500);
        builder.Property(t => t.ReferenceNo).HasMaxLength(100);
        builder.HasIndex(t => new { t.TenantId, t.BankAccountId, t.CreatedAt });
        builder.HasOne(t => t.BankAccount).WithMany(b => b.Transactions).HasForeignKey(t => t.BankAccountId)
            .OnDelete(DeleteBehavior.NoAction);
    }
}

public class ExpenseCategoryConfiguration : IEntityTypeConfiguration<ExpenseCategory>
{
    public void Configure(EntityTypeBuilder<ExpenseCategory> builder)
    {
        builder.ToTable("ExpenseCategories");
        builder.HasKey(c => c.Id);
        builder.Property(c => c.Name).HasMaxLength(100).IsRequired();
        builder.HasIndex(c => new { c.TenantId, c.Name });
        builder.HasOne(c => c.Parent).WithMany(c => c.Children).HasForeignKey(c => c.ParentId)
            .OnDelete(DeleteBehavior.NoAction);
    }
}

public class ExpenseConfiguration : IEntityTypeConfiguration<Expense>
{
    public void Configure(EntityTypeBuilder<Expense> builder)
    {
        builder.ToTable("Expenses");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Amount).HasPrecision(18, 4);
        builder.Property(e => e.Description).HasMaxLength(500).IsRequired();
        builder.HasIndex(e => new { e.TenantId, e.ExpenseDate });
        builder.HasOne(e => e.Category).WithMany().HasForeignKey(e => e.CategoryId).OnDelete(DeleteBehavior.NoAction);
    }
}

public class IncomeCategoryConfiguration : IEntityTypeConfiguration<IncomeCategory>
{
    public void Configure(EntityTypeBuilder<IncomeCategory> builder)
    {
        builder.ToTable("IncomeCategories");
        builder.HasKey(c => c.Id);
        builder.Property(c => c.Name).HasMaxLength(100).IsRequired();
        builder.HasIndex(c => new { c.TenantId, c.Name });
        builder.HasOne(c => c.Parent).WithMany(c => c.Children).HasForeignKey(c => c.ParentId)
            .OnDelete(DeleteBehavior.NoAction);
    }
}

public class IncomeConfiguration : IEntityTypeConfiguration<Income>
{
    public void Configure(EntityTypeBuilder<Income> builder)
    {
        builder.ToTable("Incomes");
        builder.HasKey(i => i.Id);
        builder.Property(i => i.Amount).HasPrecision(18, 4);
        builder.Property(i => i.Description).HasMaxLength(500).IsRequired();
        builder.Property(i => i.Source).HasMaxLength(200);
        builder.HasIndex(i => new { i.TenantId, i.IncomeDate });
        builder.HasOne(i => i.Category).WithMany().HasForeignKey(i => i.CategoryId).OnDelete(DeleteBehavior.NoAction);
    }
}

public class ReceivableConfiguration : IEntityTypeConfiguration<Receivable>
{
    public void Configure(EntityTypeBuilder<Receivable> builder)
    {
        builder.ToTable("Receivables");
        builder.HasKey(r => r.Id);
        builder.Property(r => r.Amount).HasPrecision(18, 4);
        builder.Property(r => r.PaidAmount).HasPrecision(18, 4);
        builder.HasIndex(r => new { r.TenantId, r.CustomerId, r.Status });
        builder.HasOne(r => r.Customer).WithMany().HasForeignKey(r => r.CustomerId).OnDelete(DeleteBehavior.NoAction);
    }
}

public class PayableConfiguration : IEntityTypeConfiguration<Payable>
{
    public void Configure(EntityTypeBuilder<Payable> builder)
    {
        builder.ToTable("Payables");
        builder.HasKey(p => p.Id);
        builder.Property(p => p.CreditorName).HasMaxLength(200).IsRequired();
        builder.Property(p => p.Amount).HasPrecision(18, 4);
        builder.Property(p => p.PaidAmount).HasPrecision(18, 4);
        builder.HasIndex(p => new { p.TenantId, p.Status, p.DueDate });
        builder.HasOne<Supplier>().WithMany().HasForeignKey(p => p.SupplierId).OnDelete(DeleteBehavior.NoAction);
    }
}

public class FinancePaymentConfiguration : IEntityTypeConfiguration<FinancePayment>
{
    public void Configure(EntityTypeBuilder<FinancePayment> builder)
    {
        builder.ToTable("FinancePayments");
        builder.HasKey(p => p.Id);
        builder.Property(p => p.Amount).HasPrecision(18, 4);
        builder.Property(p => p.ReferenceNo).HasMaxLength(100);
        builder.Property(p => p.Note).HasMaxLength(500);
        builder.HasIndex(p => new { p.TenantId, p.CustomerId, p.PaidAt });
        builder.HasOne(p => p.Customer).WithMany().HasForeignKey(p => p.CustomerId).OnDelete(DeleteBehavior.NoAction);
        builder.HasOne(p => p.Receivable).WithMany().HasForeignKey(p => p.ReceivableId).OnDelete(DeleteBehavior.NoAction);
        builder.HasOne(p => p.Payable).WithMany().HasForeignKey(p => p.PayableId).OnDelete(DeleteBehavior.NoAction);
    }
}

public class EmployeeConfiguration : IEntityTypeConfiguration<Employee>
{
    public void Configure(EntityTypeBuilder<Employee> builder)
    {
        builder.ToTable("Employees");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.EmployeeNo).HasMaxLength(50).IsRequired();
        builder.Property(e => e.FullName).HasMaxLength(200).IsRequired();
        builder.Property(e => e.Department).HasMaxLength(100);
        builder.Property(e => e.Position).HasMaxLength(100);
        builder.Property(e => e.Phone).HasMaxLength(30);
        builder.Property(e => e.Email).HasMaxLength(200);
        builder.Property(e => e.Salary).HasPrecision(18, 4);
        builder.HasIndex(e => new { e.TenantId, e.EmployeeNo }).IsUnique();
    }
}

public class LeaveRequestConfiguration : IEntityTypeConfiguration<LeaveRequest>
{
    public void Configure(EntityTypeBuilder<LeaveRequest> builder)
    {
        builder.ToTable("LeaveRequests");
        builder.HasKey(l => l.Id);
        builder.Property(l => l.Reason).HasMaxLength(500);
        builder.HasOne(l => l.Employee).WithMany(e => e.LeaveRequests).HasForeignKey(l => l.EmployeeId).OnDelete(DeleteBehavior.NoAction);
        builder.HasIndex(l => new { l.TenantId, l.EmployeeId, l.Status });
    }
}

public class ShiftConfiguration : IEntityTypeConfiguration<Shift>
{
    public void Configure(EntityTypeBuilder<Shift> builder)
    {
        builder.ToTable("Shifts");
        builder.HasKey(s => s.Id);
        builder.Property(s => s.Notes).HasMaxLength(500);
        builder.HasOne(s => s.Employee).WithMany(e => e.Shifts).HasForeignKey(s => s.EmployeeId).OnDelete(DeleteBehavior.NoAction);
        builder.HasOne(s => s.Branch).WithMany().HasForeignKey(s => s.BranchId).OnDelete(DeleteBehavior.NoAction);
        builder.HasIndex(s => new { s.TenantId, s.BranchId, s.Date });
    }
}

public class PayrollConfiguration : IEntityTypeConfiguration<Payroll>
{
    public void Configure(EntityTypeBuilder<Payroll> builder)
    {
        builder.ToTable("Payrolls");
        builder.HasKey(p => p.Id);
        builder.Property(p => p.Period).HasMaxLength(20).IsRequired();
        builder.Property(p => p.GrossSalary).HasPrecision(18, 4);
        builder.Property(p => p.Deductions).HasPrecision(18, 4);
        builder.Property(p => p.NetSalary).HasPrecision(18, 4);
        builder.HasOne(p => p.Employee).WithMany(e => e.Payrolls).HasForeignKey(p => p.EmployeeId).OnDelete(DeleteBehavior.NoAction);
        builder.HasIndex(p => new { p.TenantId, p.EmployeeId, p.Period }).IsUnique();
    }
}

public class SupplierConfiguration : IEntityTypeConfiguration<Supplier>
{
    public void Configure(EntityTypeBuilder<Supplier> builder)
    {
        builder.ToTable("Suppliers");
        builder.HasKey(s => s.Id);
        builder.Property(s => s.Name).HasMaxLength(200).IsRequired();
        builder.Property(s => s.TaxNumber).HasMaxLength(50);
        builder.Property(s => s.ContactPerson).HasMaxLength(200);
        builder.Property(s => s.Phone).HasMaxLength(30);
        builder.Property(s => s.Email).HasMaxLength(200);
        builder.Property(s => s.Address).HasMaxLength(500);
        builder.Property(s => s.Balance).HasPrecision(18, 4);
        builder.Property(s => s.Rating).HasPrecision(5, 2);
        builder.HasIndex(s => new { s.TenantId, s.TaxNumber });
    }
}

public class PurchaseOrderConfiguration : IEntityTypeConfiguration<PurchaseOrder>
{
    public void Configure(EntityTypeBuilder<PurchaseOrder> builder)
    {
        builder.ToTable("PurchaseOrders");
        builder.HasKey(p => p.Id);
        builder.Property(p => p.PoNumber).HasMaxLength(50).IsRequired();
        builder.Property(p => p.SubTotal).HasPrecision(18, 4);
        builder.Property(p => p.TaxTotal).HasPrecision(18, 4);
        builder.Property(p => p.GrandTotal).HasPrecision(18, 4);
        builder.HasIndex(p => new { p.TenantId, p.PoNumber }).IsUnique();
        builder.HasOne(p => p.Supplier).WithMany(s => s.PurchaseOrders).HasForeignKey(p => p.SupplierId).OnDelete(DeleteBehavior.NoAction);
        builder.HasMany(p => p.Lines).WithOne(l => l.PurchaseOrder).HasForeignKey(l => l.PurchaseOrderId).OnDelete(DeleteBehavior.Cascade);
    }
}

public class PurchaseOrderLineConfiguration : IEntityTypeConfiguration<PurchaseOrderLine>
{
    public void Configure(EntityTypeBuilder<PurchaseOrderLine> builder)
    {
        builder.ToTable("PurchaseOrderLines");
        builder.HasKey(l => l.Id);
        builder.Property(l => l.Quantity).HasPrecision(18, 4);
        builder.Property(l => l.UnitPrice).HasPrecision(18, 4);
        builder.Property(l => l.TaxRate).HasPrecision(5, 2);
        builder.Property(l => l.LineTotal).HasPrecision(18, 4);
        builder.Property(l => l.ReceivedQty).HasPrecision(18, 4);
        builder.HasOne(l => l.ProductVariant).WithMany().HasForeignKey(l => l.ProductVariantId).OnDelete(DeleteBehavior.NoAction);
    }
}

public class EInvoiceProfileConfiguration : IEntityTypeConfiguration<EInvoiceProfile>
{
    public void Configure(EntityTypeBuilder<EInvoiceProfile> builder)
    {
        builder.ToTable("EInvoiceProfiles");
        builder.HasKey(p => p.Id);
        builder.Property(p => p.ApiKey).HasMaxLength(500);
        builder.Property(p => p.ApiSecret).HasMaxLength(500);
        builder.Property(p => p.CertificatePath).HasMaxLength(500);
        builder.Property(p => p.TaxNumber).HasMaxLength(50);
        builder.Property(p => p.CompanyTitle).HasMaxLength(200);
        builder.Property(p => p.SettingsJson).HasColumnType("nvarchar(max)");
        builder.HasIndex(p => p.TenantId).IsUnique();
    }
}

public class EInvoiceSubmissionConfiguration : IEntityTypeConfiguration<EInvoiceSubmission>
{
    public void Configure(EntityTypeBuilder<EInvoiceSubmission> builder)
    {
        builder.ToTable("EInvoiceSubmissions");
        builder.HasKey(s => s.Id);
        builder.Property(s => s.GibUuid).HasMaxLength(100);
        builder.Property(s => s.ResponseJson).HasColumnType("nvarchar(max)");
        builder.Property(s => s.ErrorMessage).HasMaxLength(1000);
        builder.HasIndex(s => new { s.TenantId, s.Status, s.SubmittedAt });
        builder.HasOne(s => s.Invoice).WithMany().HasForeignKey(s => s.InvoiceId).OnDelete(DeleteBehavior.NoAction);
        builder.HasOne(s => s.Order).WithMany().HasForeignKey(s => s.OrderId).OnDelete(DeleteBehavior.NoAction);
    }
}

public class EDispatchNoteConfiguration : IEntityTypeConfiguration<EDispatchNote>
{
    public void Configure(EntityTypeBuilder<EDispatchNote> builder)
    {
        builder.ToTable("EDispatchNotes");
        builder.HasKey(d => d.Id);
        builder.Property(d => d.DispatchNumber).HasMaxLength(50).IsRequired();
        builder.Property(d => d.GibUuid).HasMaxLength(100);
        builder.Property(d => d.ResponseJson).HasColumnType("nvarchar(max)");
        builder.HasIndex(d => new { d.TenantId, d.DispatchNumber }).IsUnique();
        builder.HasOne(d => d.Order).WithMany().HasForeignKey(d => d.OrderId).OnDelete(DeleteBehavior.NoAction);
    }
}

public class NotificationConfiguration : IEntityTypeConfiguration<Notification>
{
    public void Configure(EntityTypeBuilder<Notification> builder)
    {
        builder.ToTable("Notifications");
        builder.HasKey(n => n.Id);
        builder.Property(n => n.Title).HasMaxLength(200).IsRequired();
        builder.Property(n => n.Message).HasMaxLength(2000).IsRequired();
        builder.Property(n => n.PayloadJson).HasColumnType("nvarchar(max)");
        builder.HasIndex(n => new { n.TenantId, n.UserId, n.IsRead, n.CreatedAt });
    }
}

public class OutboxMessageConfiguration : IEntityTypeConfiguration<OutboxMessage>
{
    public void Configure(EntityTypeBuilder<OutboxMessage> builder)
    {
        builder.ToTable("OutboxMessages");
        builder.HasKey(o => o.Id);
        builder.Property(o => o.EventType).HasMaxLength(100).IsRequired();
        builder.Property(o => o.Payload).HasColumnType("nvarchar(max)").IsRequired();
        builder.Property(o => o.ErrorMessage).HasMaxLength(2000);
        builder.HasIndex(o => new { o.Status, o.CreatedAt });
    }
}

public class InboxMessageConfiguration : IEntityTypeConfiguration<InboxMessage>
{
    public void Configure(EntityTypeBuilder<InboxMessage> builder)
    {
        builder.ToTable("InboxMessages");
        builder.HasKey(i => i.Id);
        builder.Property(i => i.MessageId).HasMaxLength(100).IsRequired();
        builder.HasIndex(i => i.MessageId).IsUnique();
    }
}

public class DailySalesSummaryConfiguration : IEntityTypeConfiguration<DailySalesSummary>
{
    public void Configure(EntityTypeBuilder<DailySalesSummary> builder)
    {
        builder.ToTable("DailySalesSummaries");
        builder.HasKey(s => s.Id);
        builder.Property(s => s.TotalSales).HasPrecision(18, 4);
        builder.HasIndex(s => new { s.TenantId, s.Date }).IsUnique();
    }
}

public class ProductSalesRankingConfiguration : IEntityTypeConfiguration<ProductSalesRanking>
{
    public void Configure(EntityTypeBuilder<ProductSalesRanking> builder)
    {
        builder.ToTable("ProductSalesRankings");
        builder.HasKey(r => r.Id);
        builder.Property(r => r.Period).HasMaxLength(20).IsRequired();
        builder.Property(r => r.QuantitySold).HasPrecision(18, 4);
        builder.Property(r => r.Revenue).HasPrecision(18, 4);
        builder.HasIndex(r => new { r.TenantId, r.ProductVariantId, r.Period }).IsUnique();
        builder.HasOne(r => r.ProductVariant).WithMany().HasForeignKey(r => r.ProductVariantId);
    }
}

public class BranchPerformanceSnapshotConfiguration : IEntityTypeConfiguration<BranchPerformanceSnapshot>
{
    public void Configure(EntityTypeBuilder<BranchPerformanceSnapshot> builder)
    {
        builder.ToTable("BranchPerformanceSnapshots");
        builder.HasKey(s => s.Id);
        builder.Property(s => s.Period).HasMaxLength(20).IsRequired();
        builder.Property(s => s.TotalSales).HasPrecision(18, 4);
        builder.HasIndex(s => new { s.TenantId, s.BranchId, s.Period }).IsUnique();
        builder.HasOne(s => s.Branch).WithMany().HasForeignKey(s => s.BranchId);
    }
}

public class StockAlertViewConfiguration : IEntityTypeConfiguration<StockAlertView>
{
    public void Configure(EntityTypeBuilder<StockAlertView> builder)
    {
        builder.ToTable("StockAlertViews");
        builder.HasKey(a => a.Id);
        builder.Property(a => a.Quantity).HasPrecision(18, 4);
        builder.Property(a => a.MinStock).HasPrecision(18, 4);
        builder.HasIndex(a => new { a.TenantId, a.WarehouseId, a.ProductVariantId, a.IsResolved });
        builder.HasOne(a => a.Warehouse).WithMany().HasForeignKey(a => a.WarehouseId);
        builder.HasOne(a => a.ProductVariant).WithMany().HasForeignKey(a => a.ProductVariantId);
    }
}

public class CustomerAttachmentConfiguration : IEntityTypeConfiguration<CustomerAttachment>
{
    public void Configure(EntityTypeBuilder<CustomerAttachment> builder)
    {
        builder.ToTable("CustomerAttachments");
        builder.HasKey(a => a.Id);
        builder.Property(a => a.FileName).HasMaxLength(255).IsRequired();
        builder.Property(a => a.ContentType).HasMaxLength(100).IsRequired();
        builder.Property(a => a.StoragePath).HasMaxLength(500).IsRequired();
        builder.HasIndex(a => new { a.TenantId, a.CustomerId });
        builder.HasOne(a => a.Customer).WithMany().HasForeignKey(a => a.CustomerId);
    }
}

public class PasswordResetTokenConfiguration : IEntityTypeConfiguration<PasswordResetToken>
{
    public void Configure(EntityTypeBuilder<PasswordResetToken> builder)
    {
        builder.ToTable("PasswordResetTokens");
        builder.HasKey(t => t.Id);
        builder.Property(t => t.Token).HasMaxLength(100).IsRequired();
        builder.HasIndex(t => new { t.UserId, t.Token });
        builder.HasOne(t => t.User).WithMany().HasForeignKey(t => t.UserId).OnDelete(DeleteBehavior.Cascade);
    }
}
