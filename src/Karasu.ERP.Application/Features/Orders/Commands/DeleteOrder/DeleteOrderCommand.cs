using Karasu.ERP.Shared.Models;
using MediatR;

namespace Karasu.ERP.Application.Features.Orders.Commands.DeleteOrder;

public record DeleteOrderCommand(Guid Id) : IRequest<Result>;
