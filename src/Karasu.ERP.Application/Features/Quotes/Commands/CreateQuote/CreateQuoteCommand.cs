using Karasu.ERP.Shared.Models;
using MediatR;

namespace Karasu.ERP.Application.Features.Quotes.Commands.CreateQuote;

public record CreateQuoteCommand(
    Guid? BranchId,
    Guid? CustomerId,
    string? Notes,
    DateTime? ValidUntil,
    List<CreateQuoteLineDto> Lines) : IRequest<Result<Guid>>;

public record CreateQuoteLineDto(
    Guid ProductVariantId,
    decimal Quantity,
    decimal UnitPrice,
    decimal TaxRate,
    decimal Discount = 0);
