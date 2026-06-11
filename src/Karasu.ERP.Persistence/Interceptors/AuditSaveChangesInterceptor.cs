using System.Text.Json;
using Karasu.ERP.Application.Common.Interfaces;
using Karasu.ERP.Domain.Common;
using Karasu.ERP.Domain.Entities;
using Karasu.ERP.Identity.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Karasu.ERP.Persistence.Interceptors;

public class AuditSaveChangesInterceptor : SaveChangesInterceptor
{
    private static readonly HashSet<string> ExcludedTypes =
    [
        nameof(AuditLog),
        nameof(RefreshToken),
        nameof(Permission),
        nameof(RolePermission)
    ];

    private readonly ITenantContext _tenantContext;
    private readonly ICurrentUserService _currentUser;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IDateTimeService _dateTime;

    public AuditSaveChangesInterceptor(
        ITenantContext tenantContext,
        ICurrentUserService currentUser,
        IHttpContextAccessor httpContextAccessor,
        IDateTimeService dateTime)
    {
        _tenantContext = tenantContext;
        _currentUser = currentUser;
        _httpContextAccessor = httpContextAccessor;
        _dateTime = dateTime;
    }

    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData,
        InterceptionResult<int> result)
    {
        CreateAuditEntries(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        CreateAuditEntries(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private void CreateAuditEntries(DbContext? context)
    {
        if (context is null) return;

        var httpContext = _httpContextAccessor.HttpContext;
        var ipAddress = httpContext?.Connection.RemoteIpAddress?.ToString();
        var userAgent = httpContext?.Request.Headers.UserAgent.ToString();
        var now = _dateTime.UtcNow;

        var entries = context.ChangeTracker.Entries<BaseEntity>()
            .Where(e => e.State is EntityState.Added or EntityState.Modified or EntityState.Deleted)
            .Where(e => e.Entity is not AuditLog)
            .Where(e => !ExcludedTypes.Contains(e.Entity.GetType().Name))
            .ToList();

        foreach (var entry in entries)
        {
            var action = ResolveAction(entry);

            var auditLog = new AuditLog
            {
                Id = Guid.NewGuid(),
                TenantId = ResolveTenantId(entry.Entity),
                UserId = _currentUser.UserId,
                EntityType = entry.Entity.GetType().Name,
                EntityId = entry.Entity.Id,
                Action = action,
                OldValues = action is "Update" or "Delete" ? SerializeValues(entry, useOriginal: true) : null,
                NewValues = action is "Create" or "Update" ? SerializeValues(entry, useOriginal: false) : null,
                IpAddress = ipAddress,
                UserAgent = userAgent,
                CreatedAt = now
            };

            context.Set<AuditLog>().Add(auditLog);
        }
    }

    private Guid ResolveTenantId(BaseEntity entity) => entity switch
    {
        Tenant tenant => tenant.Id,
        TenantEntity tenantEntity when tenantEntity.TenantId != Guid.Empty => tenantEntity.TenantId,
        TenantEntity => _tenantContext.TenantId,
        _ => _tenantContext.TenantId
    };

    private static string ResolveAction(Microsoft.EntityFrameworkCore.ChangeTracking.EntityEntry entry)
    {
        if (entry.State == EntityState.Added) return "Create";
        if (entry.State == EntityState.Deleted) return "Delete";

        if (entry.State == EntityState.Modified
            && entry.Entity is BaseEntity { IsDeleted: true }
            && entry.Property(nameof(BaseEntity.IsDeleted)).IsModified)
            return "Delete";

        return "Update";
    }

    private static string? SerializeValues(Microsoft.EntityFrameworkCore.ChangeTracking.EntityEntry entry, bool useOriginal)
    {
        var values = new Dictionary<string, object?>();

        foreach (var property in entry.Properties)
        {
            if (property.Metadata.IsPrimaryKey()) continue;
            if (property.Metadata.IsForeignKey()) continue;
            if (entry.Metadata.FindNavigation(property.Metadata.Name) is not null) continue;

            if (useOriginal && !property.IsModified && entry.State == EntityState.Modified)
                continue;

            var value = useOriginal ? property.OriginalValue : property.CurrentValue;
            values[property.Metadata.Name] = value;
        }

        return values.Count == 0 ? null : JsonSerializer.Serialize(values);
    }
}
