using Karasu.ERP.Domain.Common;
using Karasu.ERP.Domain.Enums;

namespace Karasu.ERP.Domain.Entities;

public class Employee : TenantEntity, IAggregateRoot
{
    public Guid? UserId { get; set; }
    public string EmployeeNo { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string? Department { get; set; }
    public string? Position { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public DateTime HireDate { get; set; }
    public decimal Salary { get; set; }
    public EmployeeStatus Status { get; set; } = EmployeeStatus.Active;

    public ICollection<LeaveRequest> LeaveRequests { get; set; } = new List<LeaveRequest>();
    public ICollection<Shift> Shifts { get; set; } = new List<Shift>();
    public ICollection<Payroll> Payrolls { get; set; } = new List<Payroll>();

    public IReadOnlyCollection<IDomainEvent> DomainEvents => Array.Empty<IDomainEvent>();
    public void ClearDomainEvents() { }
}

public class LeaveRequest : TenantEntity
{
    public Guid EmployeeId { get; set; }
    public LeaveType Type { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string? Reason { get; set; }
    public LeaveRequestStatus Status { get; set; } = LeaveRequestStatus.Pending;
    public Guid? ApprovedBy { get; set; }
    public DateTime? ApprovedAt { get; set; }

    public Employee Employee { get; set; } = null!;

    public void Approve(Guid approvedBy)
    {
        if (Status != LeaveRequestStatus.Pending)
            throw new InvalidOperationException("Sadece bekleyen izin talepleri onaylanabilir.");

        Status = LeaveRequestStatus.Approved;
        ApprovedBy = approvedBy;
        ApprovedAt = DateTime.UtcNow;
    }

    public void Reject(Guid rejectedBy)
    {
        if (Status != LeaveRequestStatus.Pending)
            throw new InvalidOperationException("Sadece bekleyen izin talepleri reddedilebilir.");

        Status = LeaveRequestStatus.Rejected;
        ApprovedBy = rejectedBy;
        ApprovedAt = DateTime.UtcNow;
    }
}

public class Shift : TenantEntity
{
    public Guid EmployeeId { get; set; }
    public Guid BranchId { get; set; }
    public DateTime Date { get; set; }
    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }
    public string? Notes { get; set; }

    public Employee Employee { get; set; } = null!;
    public Branch Branch { get; set; } = null!;
}

public class Payroll : TenantEntity
{
    public Guid EmployeeId { get; set; }
    public string Period { get; set; } = string.Empty;
    public decimal GrossSalary { get; set; }
    public decimal Deductions { get; set; }
    public decimal NetSalary { get; private set; }
    public PayrollStatus Status { get; set; } = PayrollStatus.Generated;
    public DateTime? PaidAt { get; set; }

    public Employee Employee { get; set; } = null!;

    public static Payroll Generate(Guid tenantId, Guid employeeId, string period, decimal grossSalary, decimal deductions)
    {
        return new Payroll
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            EmployeeId = employeeId,
            Period = period,
            GrossSalary = grossSalary,
            Deductions = deductions,
            NetSalary = grossSalary - deductions,
            Status = PayrollStatus.Generated,
            CreatedAt = DateTime.UtcNow
        };
    }

    public void MarkPaid()
    {
        if (Status == PayrollStatus.Paid)
            throw new InvalidOperationException("Bordro zaten ödendi.");

        Status = PayrollStatus.Paid;
        PaidAt = DateTime.UtcNow;
    }
}
