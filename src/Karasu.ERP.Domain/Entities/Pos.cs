using Karasu.ERP.Domain.Common;
using Karasu.ERP.Domain.Enums;

namespace Karasu.ERP.Domain.Entities;

public class PosSession : TenantEntity
{
    public Guid BranchId { get; set; }
    public Guid CashierId { get; set; }
    public DateTime OpenedAt { get; set; }
    public DateTime? ClosedAt { get; set; }
    public decimal OpeningBalance { get; set; }
    public decimal? ClosingBalance { get; set; }
    public PosSessionStatus Status { get; set; } = PosSessionStatus.Open;

    public Branch Branch { get; set; } = null!;
    public ICollection<PosTransaction> Transactions { get; set; } = new List<PosTransaction>();

    public static PosSession Open(Guid tenantId, Guid branchId, Guid cashierId, decimal openingBalance) =>
        new()
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            BranchId = branchId,
            CashierId = cashierId,
            OpeningBalance = openingBalance,
            OpenedAt = DateTime.UtcNow,
            Status = PosSessionStatus.Open,
            CreatedAt = DateTime.UtcNow
        };

    public void Close(decimal closingBalance)
    {
        if (Status == PosSessionStatus.Closed)
            throw new InvalidOperationException("Kasa oturumu zaten kapatılmış.");

        Status = PosSessionStatus.Closed;
        ClosingBalance = closingBalance;
        ClosedAt = DateTime.UtcNow;
    }
}

public class PosTransaction : TenantEntity
{
    public Guid SessionId { get; set; }
    public Guid OrderId { get; set; }
    public PaymentMethod PaymentMethod { get; set; }
    public decimal Amount { get; set; }
    public decimal ChangeAmount { get; set; }

    public PosSession Session { get; set; } = null!;
    public Order Order { get; set; } = null!;
}

public class PosReturn : TenantEntity
{
    public Guid SessionId { get; set; }
    public Guid OriginalOrderId { get; set; }
    public string? Reason { get; set; }
    public decimal RefundAmount { get; set; }
    public PaymentMethod RefundMethod { get; set; }

    public PosSession Session { get; set; } = null!;
    public Order OriginalOrder { get; set; } = null!;
}
