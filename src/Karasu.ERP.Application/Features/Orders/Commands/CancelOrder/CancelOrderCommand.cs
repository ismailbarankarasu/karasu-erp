using Karasu.ERP.Shared.Models;
using MediatR;

namespace Karasu.ERP.Application.Features.Orders.Commands.CancelOrder;

public record CancelOrderCommand(Guid OrderId, string? Reason) : IRequest<Result>;
