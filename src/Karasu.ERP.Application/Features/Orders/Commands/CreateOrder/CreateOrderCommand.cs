using Karasu.ERP.Shared.Models;
using MediatR;

namespace Karasu.ERP.Application.Features.Orders.Commands.CreateOrder;

public record CreateOrderCommand(
    Guid BranchId,
    Guid? CustomerId,
    string? Notes,
    List<CreateOrderLineDto> Lines) : IRequest<Result<Guid>>;

public record CreateOrderLineDto(
    Guid ProductVariantId,
    decimal Quantity,
    decimal UnitPrice,
    decimal TaxRate,
    decimal Discount = 0);
