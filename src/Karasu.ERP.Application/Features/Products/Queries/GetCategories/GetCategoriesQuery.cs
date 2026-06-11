using Karasu.ERP.Shared.Models;
using MediatR;

namespace Karasu.ERP.Application.Features.Products.Queries.GetCategories;

public record GetCategoriesQuery : IRequest<Result<IReadOnlyList<CategoryDto>>>;

public record CategoryDto(Guid Id, string Name, Guid? ParentId, int SortOrder);
