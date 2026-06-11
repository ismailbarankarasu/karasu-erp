using Karasu.ERP.Shared.Models;
using MediatR;

namespace Karasu.ERP.Application.Features.Products.Queries.GetBrands;

public record GetBrandsQuery : IRequest<Result<IReadOnlyList<BrandDto>>>;

public record BrandDto(Guid Id, string Name);
