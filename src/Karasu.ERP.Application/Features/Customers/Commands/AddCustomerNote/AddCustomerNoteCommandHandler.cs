using Karasu.ERP.Application.Common.Interfaces;
using Karasu.ERP.Domain.Entities;
using Karasu.ERP.Shared.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Karasu.ERP.Application.Features.Customers.Commands.AddCustomerNote;

public class AddCustomerNoteCommandHandler : IRequestHandler<AddCustomerNoteCommand, Result<Guid>>
{
    private readonly IApplicationDbContext _context;
    private readonly ITenantContext _tenantContext;
    private readonly ICurrentUserService _currentUser;

    public AddCustomerNoteCommandHandler(
        IApplicationDbContext context,
        ITenantContext tenantContext,
        ICurrentUserService currentUser)
    {
        _context = context;
        _tenantContext = tenantContext;
        _currentUser = currentUser;
    }

    public async Task<Result<Guid>> Handle(AddCustomerNoteCommand request, CancellationToken cancellationToken)
    {
        var customerExists = await _context.Customers
            .AnyAsync(
                c => c.Id == request.Id && c.TenantId == _tenantContext.TenantId && !c.IsDeleted,
                cancellationToken);

        if (!customerExists)
            return Result<Guid>.Failure("Müşteri bulunamadı.", "CUSTOMER_NOT_FOUND");

        var note = new CustomerNote
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantContext.TenantId,
            CustomerId = request.Id,
            Content = request.Content,
            CreatedByUserId = _currentUser.UserId ?? Guid.Empty,
            CreatedAt = DateTime.UtcNow
        };

        await _context.CustomerNotes.AddAsync(note, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        return Result<Guid>.Success(note.Id);
    }
}
