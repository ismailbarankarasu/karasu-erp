using Karasu.ERP.Domain.Enums;
using Karasu.ERP.Shared.Models;
using MediatR;

namespace Karasu.ERP.Application.Features.Quotes.Queries.GetQuotes;

public record GetQuotesQuery(
    int Page = 1,
    int PageSize = 20,
    QuoteStatus? Status = null,
    Guid? CustomerId = null) : IRequest<Result<PaginatedList<QuoteListDto>>>;

public record QuoteListDto(
    Guid Id,
    string QuoteNumber,
    QuoteStatus Status,
    Guid? CustomerId,
    string? CustomerName,
    decimal GrandTotal,
    DateTime? ValidUntil,
    DateTime CreatedAt);
