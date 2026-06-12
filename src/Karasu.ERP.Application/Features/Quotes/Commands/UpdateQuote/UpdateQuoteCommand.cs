using Karasu.ERP.Application.Features.Quotes.Commands.CreateQuote;
using Karasu.ERP.Shared.Models;
using MediatR;

namespace Karasu.ERP.Application.Features.Quotes.Commands.UpdateQuote;

public record UpdateQuoteCommand(
    Guid Id,
    Guid? BranchId,
    Guid? CustomerId,
    string? Notes,
    DateTime? ValidUntil,
    List<CreateQuoteLineDto> Lines) : IRequest<Result>;
