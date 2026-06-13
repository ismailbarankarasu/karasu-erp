using Karasu.ERP.Application.Common.Interfaces;
using Karasu.ERP.Domain.Entities;
using Karasu.ERP.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Karasu.ERP.Application.Features.Finance.Services;

public static class FinanceReceivableService
{
    public static async Task CreateForIssuedInvoiceAsync(
        IApplicationDbContext context,
        Invoice invoice,
        CancellationToken cancellationToken)
    {
        var alreadyExists = await context.Receivables.AnyAsync(
            r => r.InvoiceId == invoice.Id && !r.IsDeleted,
            cancellationToken);

        if (alreadyExists)
            return;

        var customer = await context.Customers
            .FirstOrDefaultAsync(
                c => c.Id == invoice.CustomerId && c.TenantId == invoice.TenantId && !c.IsDeleted,
                cancellationToken);

        if (customer is null)
            return;

        var receivable = new Receivable
        {
            Id = Guid.NewGuid(),
            TenantId = invoice.TenantId,
            CustomerId = invoice.CustomerId,
            InvoiceId = invoice.Id,
            OrderId = invoice.OrderId,
            Amount = invoice.GrandTotal,
            DueDate = DateTime.UtcNow.AddDays(30),
            Status = ReceivableStatus.Open,
            Description = $"Fatura {invoice.InvoiceNumber}",
            CreatedAt = DateTime.UtcNow
        };

        customer.Balance += invoice.GrandTotal;
        await context.Receivables.AddAsync(receivable, cancellationToken);
    }
}
