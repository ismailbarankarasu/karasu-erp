using Karasu.ERP.Domain.Entities;

using Karasu.ERP.Domain.Common;

namespace Karasu.ERP.Domain.Entities;

public class CustomerAttachment : TenantEntity
{
    public Guid CustomerId { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public string StoragePath { get; set; } = string.Empty;

    public Customer Customer { get; set; } = null!;
}
