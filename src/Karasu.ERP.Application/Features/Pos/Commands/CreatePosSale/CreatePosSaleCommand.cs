using Karasu.ERP.Application.Features.Orders.Commands.CreateOrder;
using Karasu.ERP.Domain.Enums;
using Karasu.ERP.Shared.Models;
using MediatR;

namespace Karasu.ERP.Application.Features.Pos.Commands.CreatePosSale;

public record CreatePosSaleCommand(
    Guid SessionId,
    Guid? CustomerId,
    List<CreateOrderLineDto> Lines,
    List<PosPaymentDto> Payments) : IRequest<Result<PosSaleResultDto>>;

public record PosPaymentDto(
    PaymentMethod Method,
    decimal Amount,
    decimal ChangeAmount = 0);

public record PosSaleResultDto(
    Guid OrderId,
    string OrderNumber,
    decimal GrandTotal,
    int PaymentCount);
