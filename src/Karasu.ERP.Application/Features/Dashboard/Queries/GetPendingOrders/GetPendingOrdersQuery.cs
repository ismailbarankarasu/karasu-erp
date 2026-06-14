using Karasu.ERP.Domain.Enums;
using Karasu.ERP.Shared.Models;
using MediatR;

namespace Karasu.ERP.Application.Features.Dashboard.Queries.GetPendingOrders;

public record GetPendingOrdersQuery() : IRequest<Result<List<PendingOrderDto>>>;

public record PendingOrderDto(
    Guid Id,
    string OrderNumber,
    OrderStatus Status,
    string? CustomerName,
    decimal GrandTotal,
    DateTime CreatedAt);
