using Karasu.ERP.Shared.Models;
using MediatR;

namespace Karasu.ERP.Application.Features.Customers.Commands.AddCustomerNote;

public record AddCustomerNoteCommand(Guid Id, string Content) : IRequest<Result<Guid>>;
