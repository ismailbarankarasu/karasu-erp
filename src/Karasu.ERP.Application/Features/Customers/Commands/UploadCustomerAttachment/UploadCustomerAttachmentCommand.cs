using Karasu.ERP.Application.Common.Interfaces;
using Karasu.ERP.Domain.Entities;
using Karasu.ERP.Shared.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Karasu.ERP.Application.Features.Customers.Commands.UploadCustomerAttachment;

public record UploadCustomerAttachmentCommand(
    Guid CustomerId,
    Stream FileStream,
    string FileName,
    string ContentType,
    long FileSize) : IRequest<Result<Guid>>;

public class UploadCustomerAttachmentCommandHandler : IRequestHandler<UploadCustomerAttachmentCommand, Result<Guid>>
{
    private const long MaxFileSize = 10 * 1024 * 1024;

    private readonly IApplicationDbContext _context;
    private readonly ITenantContext _tenantContext;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IFileStorageService _fileStorage;

    public UploadCustomerAttachmentCommandHandler(
        IApplicationDbContext context,
        ITenantContext tenantContext,
        IUnitOfWork unitOfWork,
        IFileStorageService fileStorage)
    {
        _context = context;
        _tenantContext = tenantContext;
        _unitOfWork = unitOfWork;
        _fileStorage = fileStorage;
    }

    public async Task<Result<Guid>> Handle(UploadCustomerAttachmentCommand request, CancellationToken cancellationToken)
    {
        if (request.FileSize <= 0 || request.FileSize > MaxFileSize)
            return Result<Guid>.Failure("Dosya boyutu geçersiz (max 10 MB).", "INVALID_FILE_SIZE");

        var customerExists = await _context.Customers.AnyAsync(
            c => c.Id == request.CustomerId &&
                 c.TenantId == _tenantContext.TenantId &&
                 !c.IsDeleted,
            cancellationToken);

        if (!customerExists)
            return Result<Guid>.Failure("Müşteri bulunamadı.", "CUSTOMER_NOT_FOUND");

        var folder = $"customers/{request.CustomerId:N}";
        var storagePath = await _fileStorage.SaveAsync(
            request.FileStream,
            request.FileName,
            folder,
            cancellationToken);

        var attachment = new CustomerAttachment
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantContext.TenantId,
            CustomerId = request.CustomerId,
            FileName = request.FileName,
            ContentType = request.ContentType,
            FileSize = request.FileSize,
            StoragePath = storagePath,
            CreatedAt = DateTime.UtcNow
        };

        await _context.CustomerAttachments.AddAsync(attachment, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result<Guid>.Success(attachment.Id);
    }
}
