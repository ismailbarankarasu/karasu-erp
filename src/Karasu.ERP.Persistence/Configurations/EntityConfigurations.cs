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
