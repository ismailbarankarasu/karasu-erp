using Karasu.ERP.Application.Common.Interfaces;
using Karasu.ERP.Domain.Enums;
using Karasu.ERP.Shared.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Karasu.ERP.Application.Features.EInvoice.Queries.GetEInvoiceSubmissions;

public record GetEInvoiceSubmissionsQuery(
    int Page = 1,
    int PageSize = 20,
    EInvoiceSubmissionType? Type = null) : IRequest<Result<PaginatedList<EInvoiceSubmissionDto>>>;

public record EInvoiceSubmissionDto(
    Guid Id,
    Guid? InvoiceId,
    Guid? OrderId,
    EInvoiceSubmissionType Type,
    EInvoiceSubmissionStatus Status,
    string? GibUuid,
    DateTime? SubmittedAt,
    string? ErrorMessage);

public class GetEInvoiceSubmissionsQueryHandler : IRequestHandler<GetEInvoiceSubmissionsQuery, Result<PaginatedList<EInvoiceSubmissionDto>>>
{
    private readonly IApplicationDbContext _context;
    private readonly ITenantContext _tenantContext;

    public GetEInvoiceSubmissionsQueryHandler(IApplicationDbContext context, ITenantContext tenantContext)
    {
        _context = context;
        _tenantContext = tenantContext;
    }

    public async Task<Result<PaginatedList<EInvoiceSubmissionDto>>> Handle(
        GetEInvoiceSubmissionsQuery request,
        CancellationToken cancellationToken)
    {
        var query = _context.EInvoiceSubmissions
            .AsNoTracking()
            .Where(s => s.TenantId == _tenantContext.TenantId && !s.IsDeleted);

        if (request.Type.HasValue)
            query = query.Where(s => s.Type == request.Type.Value);

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(s => s.SubmittedAt ?? s.CreatedAt)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(s => new EInvoiceSubmissionDto(
                s.Id, s.InvoiceId, s.OrderId, s.Type, s.Status, s.GibUuid, s.SubmittedAt, s.ErrorMessage))
            .ToListAsync(cancellationToken);

        return Result<PaginatedList<EInvoiceSubmissionDto>>.Success(
            new PaginatedList<EInvoiceSubmissionDto>(items, totalCount, request.Page, request.PageSize));
    }
}
