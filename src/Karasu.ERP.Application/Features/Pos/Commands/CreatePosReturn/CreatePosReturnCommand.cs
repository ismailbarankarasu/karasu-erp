using Karasu.ERP.Domain.Enums;
using Karasu.ERP.Shared.Models;
using MediatR;

namespace Karasu.ERP.Application.Features.Pos.Commands.CreatePosReturn;

public record CreatePosReturnCommand(
    Guid SessionId,
    Guid OriginalOrderId,
    PaymentMethod RefundMethod,
    string? Reason) : IRequest<Result<PosReturnResultDto>>;

public record PosReturnResultDto(
    Guid ReturnId,
    Guid OrderId,
    decimal RefundAmount,
    PaymentMethod RefundMethod);
