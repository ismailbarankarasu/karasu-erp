using Karasu.ERP.Shared.Models;
using MediatR;

namespace Karasu.ERP.Application.Features.Products.Commands.CreateBrand;

public record CreateBrandCommand(string Name) : IRequest<Result<Guid>>;
