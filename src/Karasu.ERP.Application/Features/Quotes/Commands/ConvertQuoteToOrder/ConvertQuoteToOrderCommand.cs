using Karasu.ERP.Shared.Models;
using MediatR;

namespace Karasu.ERP.Application.Features.Quotes.Commands.ConvertQuoteToOrder;

public record ConvertQuoteToOrderCommand(Guid QuoteId, Guid BranchId) : IRequest<Result<Guid>>;
