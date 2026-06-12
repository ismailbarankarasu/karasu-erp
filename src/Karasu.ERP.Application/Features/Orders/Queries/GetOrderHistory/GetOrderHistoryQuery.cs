using Karasu.ERP.Application.Features.Orders.Queries.GetOrderById;
using Karasu.ERP.Shared.Models;
using MediatR;

namespace Karasu.ERP.Application.Features.Orders.Queries.GetOrderHistory;

public record GetOrderHistoryQuery(Guid OrderId) : IRequest<Result<List<OrderStatusHistoryDto>>>;
