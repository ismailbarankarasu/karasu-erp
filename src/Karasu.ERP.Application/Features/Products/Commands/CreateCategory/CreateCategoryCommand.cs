using Karasu.ERP.Shared.Models;
using MediatR;

namespace Karasu.ERP.Application.Features.Products.Commands.CreateCategory;

public record CreateCategoryCommand(string Name, Guid? ParentId, int SortOrder) : IRequest<Result<Guid>>;
