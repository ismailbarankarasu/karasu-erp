using Karasu.ERP.Shared.Models;
using MediatR;

namespace Karasu.ERP.Application.Features.Stock.Commands.CompleteStockTransfer;

public record CompleteStockTransferCommand(Guid TransferId) : IRequest<Result>;
