using Karasu.ERP.Domain.Enums;
using Karasu.ERP.Shared.Models;
using MediatR;

namespace Karasu.ERP.Application.Features.Orders.Queries.GetOrders;

public record GetOrdersQuery(
    int Page = 1,
    int PageSize = 20,
    OrderStatus? Status = null,
    Guid? CustomerId = null,
    Guid? BranchId = null,
    DateTime? FromDate = null,
    DateTime? ToDate = null,
    string? SearchTerm = null) : IRequest<Result<PaginatedList<OrderListDto>>>;

public record OrderListDto(
    Guid Id,
    string OrderNumber,
    OrderStatus Status,
    string? CustomerName,
    decimal GrandTotal,
    DateTime CreatedAt);
