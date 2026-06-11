using Karasu.ERP.Shared.Models;
using MediatR;

namespace Karasu.ERP.Application.Features.Pos.Queries.PrintPosReceipt;

public record PrintPosReceiptQuery(Guid OrderId) : IRequest<Result<PosReceiptPdfDto>>;

public record PosReceiptPdfDto(byte[] Content, string FileName);
