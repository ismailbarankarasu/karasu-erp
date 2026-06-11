using Karasu.ERP.Domain.Common;
using Karasu.ERP.Domain.Enums;

namespace Karasu.ERP.Domain.Entities;

public class Customer : TenantEntity, IAggregateRoot
{
    public CustomerType Type { get; set; } = CustomerType.Individual;
    public string FullName { get; set; } = string.Empty;
    public string? CompanyName { get; set; }
    public string? TaxNumber { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? Address { get; set; }
    public string? City { get; set; }
    public decimal Balance { get; set; }
    public decimal CreditLimit { get; set; }
    public CustomerStatus Status { get; set; } = CustomerStatus.Active;

    public ICollection<Order> Orders { get; set; } = new List<Order>();
    public ICollection<CustomerNote> Notes { get; set; } = new List<CustomerNote>();

    public IReadOnlyCollection<IDomainEvent> DomainEvents => Array.Empty<IDomainEvent>();
    public void ClearDomainEvents() { }
}

public class CustomerNote : TenantEntity
{
    public Guid CustomerId { get; set; }
    public string Content { get; set; } = string.Empty;
    public Guid CreatedByUserId { get; set; }

    public Customer Customer { get; set; } = null!;
}
