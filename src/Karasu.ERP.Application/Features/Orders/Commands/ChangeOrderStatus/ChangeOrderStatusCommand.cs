using Karasu.ERP.Domain.Enums;
using Karasu.ERP.Shared.Models;
using MediatR;

namespace Karasu.ERP.Application.Features.Orders.Commands.ChangeOrderStatus;

public record ChangeOrderStatusCommand(Guid Id, OrderStatus Status, string? Note) : IRequest<Result>;
