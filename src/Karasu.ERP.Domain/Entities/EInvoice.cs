using Karasu.ERP.Domain.Common;
using Karasu.ERP.Domain.Enums;

namespace Karasu.ERP.Domain.Entities;

public class EInvoiceProfile : TenantEntity
{
    public EInvoiceProvider Provider { get; set; } = EInvoiceProvider.Stub;
    public string? ApiKey { get; set; }
    public string? ApiSecret { get; set; }
    public string? CertificatePath { get; set; }
    public string? TaxNumber { get; set; }
    public string? CompanyTitle { get; set; }
    public bool IsActive { get; set; }
    public string SettingsJson { get; set; } = "{}";
}

public class EInvoiceSubmission : TenantEntity
{
    public Guid? InvoiceId { get; set; }
    public Guid? OrderId { get; set; }
    public EInvoiceSubmissionType Type { get; set; }
    public EInvoiceSubmissionStatus Status { get; set; } = EInvoiceSubmissionStatus.Pending;
    public string? GibUuid { get; set; }
    public string? ResponseJson { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTime? SubmittedAt { get; set; }

    public Invoice? Invoice { get; set; }
    public Order? Order { get; set; }
}

public class EDispatchNote : TenantEntity
{
    public Guid OrderId { get; set; }
    public string DispatchNumber { get; set; } = string.Empty;
    public EDispatchStatus Status { get; set; } = EDispatchStatus.Draft;
    public string? GibUuid { get; set; }
    public string? ResponseJson { get; set; }
    public DateTime? SubmittedAt { get; set; }

    public Order Order { get; set; } = null!;
}
