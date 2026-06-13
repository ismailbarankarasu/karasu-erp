using Karasu.ERP.Shared.Models;
using MediatR;

namespace Karasu.ERP.Application.Features.Customers.Queries.GetCustomerNotes;

public record GetCustomerNotesQuery(Guid CustomerId) : IRequest<Result<IReadOnlyList<CustomerNoteDto>>>;

public record CustomerNoteDto(
    Guid Id,
    string Content,
    Guid CreatedByUserId,
    DateTime CreatedAt);
