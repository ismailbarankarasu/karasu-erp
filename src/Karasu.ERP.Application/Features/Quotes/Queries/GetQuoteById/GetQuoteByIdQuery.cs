using Karasu.ERP.Domain.Enums;
using Karasu.ERP.Shared.Models;
using MediatR;

namespace Karasu.ERP.Application.Features.Quotes.Queries.GetQuoteById;

public record GetQuoteByIdQuery(Guid Id) : IRequest<Result<QuoteDetailDto>>;

public record QuoteDetailDto(
    Guid Id,
    string QuoteNumber,
    QuoteStatus Status,
    Guid? BranchId,
    Guid? CustomerId,
    string? CustomerName,
    decimal SubTotal,
    decimal TaxTotal,
    decimal DiscountTotal,
    decimal GrandTotal,
    DateTime? ValidUntil,
    string? Notes,
    Guid? ConvertedOrderId,
    DateTime CreatedAt,
    IReadOnlyList<QuoteLineDto> Lines);

public record QuoteLineDto(
    Guid Id,
    Guid ProductVariantId,
    string ProductName,
    string Sku,
    decimal Quantity,
    decimal UnitPrice,
    decimal TaxRate,
    decimal Discount,
    decimal LineTotal);
