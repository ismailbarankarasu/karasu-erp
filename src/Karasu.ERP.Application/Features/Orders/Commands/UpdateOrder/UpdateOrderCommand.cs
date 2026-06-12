using Karasu.ERP.Application.Features.Orders.Commands.CreateOrder;
using Karasu.ERP.Shared.Models;
using MediatR;

namespace Karasu.ERP.Application.Features.Orders.Commands.UpdateOrder;

public record UpdateOrderCommand(
    Guid Id,
    Guid? CustomerId,
    string? Notes,
    List<CreateOrderLineDto> Lines) : IRequest<Result>;
