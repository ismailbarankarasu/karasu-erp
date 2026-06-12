using Karasu.ERP.Shared.Models;
using MediatR;

namespace Karasu.ERP.Application.Features.Stock.Commands.CompleteStockCount;

public record CompleteStockCountCommand(Guid CountId) : IRequest<Result>;
