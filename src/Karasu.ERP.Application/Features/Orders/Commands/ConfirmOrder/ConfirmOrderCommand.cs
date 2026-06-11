using Karasu.ERP.Shared.Models;
using MediatR;

namespace Karasu.ERP.Application.Features.Orders.Commands.ConfirmOrder;

public record ConfirmOrderCommand(Guid OrderId) : IRequest<Result>;
