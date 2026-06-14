using Karasu.ERP.Application.Common.Interfaces;
using Karasu.ERP.Domain.Entities;
using Karasu.ERP.Domain.Events;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Karasu.ERP.Application.Features.Dashboard.EventHandlers;

public class ReadModelProjectionHandler : INotificationHandler<OrderConfirmedEvent>
{
    private readonly IApplicationDbContext _context;

    public ReadModelProjectionHandler(IApplicationDbContext context) => _context = context;

    public async Task Handle(OrderConfirmedEvent notification, CancellationToken cancellationToken)
    {
        var orderTotal = notification.Lines.Sum(l => l.Quantity * l.UnitPrice);
        var orderDate = notification.OccurredOn.Date;
        var monthPeriod = orderDate.ToString("yyyy-MM");

        await UpsertDailySalesSummaryAsync(notification.TenantId, orderDate, orderTotal, cancellationToken);
        await UpsertBranchPerformanceAsync(
            notification.TenantId,
            notification.BranchId,
            monthPeriod,
            orderTotal,
            cancellationToken);

        foreach (var line in notification.Lines)
        {
            await UpsertProductSalesRankingAsync(
                notification.TenantId,
                line.ProductVariantId,
                monthPeriod,
                line.Quantity,
                line.Quantity * line.UnitPrice,
                cancellationToken);
        }
    }

    private async Task UpsertDailySalesSummaryAsync(
        Guid tenantId,
        DateTime date,
        decimal orderTotal,
        CancellationToken ct)
    {
        var summary = await _context.DailySalesSummaries
            .FirstOrDefaultAsync(s => s.TenantId == tenantId && s.Date == date && !s.IsDeleted, ct);

        if (summary is null)
        {
            await _context.DailySalesSummaries.AddAsync(new DailySalesSummary
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                Date = date,
                TotalSales = orderTotal,
                OrderCount = 1,
                CreatedAt = DateTime.UtcNow
            }, ct);
            return;
        }

        summary.TotalSales += orderTotal;
        summary.OrderCount += 1;
        summary.UpdatedAt = DateTime.UtcNow;
    }

    private async Task UpsertBranchPerformanceAsync(
        Guid tenantId,
        Guid branchId,
        string period,
        decimal orderTotal,
        CancellationToken ct)
    {
        var snapshot = await _context.BranchPerformanceSnapshots
            .FirstOrDefaultAsync(s =>
                s.TenantId == tenantId &&
                s.BranchId == branchId &&
                s.Period == period &&
                !s.IsDeleted,
                ct);

        if (snapshot is null)
        {
            await _context.BranchPerformanceSnapshots.AddAsync(new BranchPerformanceSnapshot
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                BranchId = branchId,
                Period = period,
                TotalSales = orderTotal,
                OrderCount = 1,
                CreatedAt = DateTime.UtcNow
            }, ct);
            return;
        }

        snapshot.TotalSales += orderTotal;
        snapshot.OrderCount += 1;
        snapshot.UpdatedAt = DateTime.UtcNow;
    }

    private async Task UpsertProductSalesRankingAsync(
        Guid tenantId,
        Guid productVariantId,
        string period,
        decimal quantity,
        decimal revenue,
        CancellationToken ct)
    {
        var ranking = await _context.ProductSalesRankings
            .FirstOrDefaultAsync(r =>
                r.TenantId == tenantId &&
                r.ProductVariantId == productVariantId &&
                r.Period == period &&
                !r.IsDeleted,
                ct);

        if (ranking is null)
        {
            await _context.ProductSalesRankings.AddAsync(new ProductSalesRanking
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                ProductVariantId = productVariantId,
                Period = period,
                QuantitySold = quantity,
                Revenue = revenue,
                CreatedAt = DateTime.UtcNow
            }, ct);
            return;
        }

        ranking.QuantitySold += quantity;
        ranking.Revenue += revenue;
        ranking.UpdatedAt = DateTime.UtcNow;
    }
}
