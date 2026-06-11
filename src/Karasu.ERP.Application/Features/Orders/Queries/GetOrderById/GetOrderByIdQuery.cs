using Karasu.ERP.Domain.Enums;
using Karasu.ERP.Shared.Models;
using MediatR;

namespace Karasu.ERP.Application.Features.Orders.Queries.GetOrderById;

public record GetOrderByIdQuery(Guid Id) : IRequest<Result<OrderDetailDto>>;

public record OrderDetailDto(
    Guid Id,
    string OrderNumber,
    OrderStatus Status,
    OrderType Type,
    Guid BranchId,
    string BranchName,
    Guid? CustomerId,
    string? CustomerName,
    decimal SubTotal,
    decimal TaxTotal,
    decimal DiscountTotal,
    decimal GrandTotal,
    string? Notes,
    DateTime CreatedAt,
    DateTime? UpdatedAt,
    IReadOnlyList<OrderLineDetailDto> Lines,
    IReadOnlyList<OrderStatusHistoryDto> StatusHistory);

public record OrderLineDetailDto(
    Guid Id,
    Guid ProductVariantId,
    string ProductName,
    string VariantSku,
    decimal Quantity,
    decimal UnitPrice,
    decimal TaxRate,
    decimal Discount,
    decimal LineTotal);

public record OrderStatusHistoryDto(
    OrderStatus FromStatus,
    OrderStatus ToStatus,
    Guid ChangedBy,
    DateTime ChangedAt,
    string? Note);
