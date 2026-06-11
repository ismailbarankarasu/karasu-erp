using Karasu.ERP.Domain.Enums;
using Karasu.ERP.Shared.Models;
using MediatR;

namespace Karasu.ERP.Application.Features.Products.Queries.GetProducts;

public record GetProductsQuery(
    int Page = 1,
    int PageSize = 20,
    string? SearchTerm = null,
    Guid? CategoryId = null,
    ProductStatus? Status = null) : IRequest<Result<PaginatedList<ProductListDto>>>;

public record ProductListDto(
    Guid Id,
    string Sku,
    string? Barcode,
    string Name,
    string? CategoryName,
    string? BrandName,
    string UnitSymbol,
    decimal SalePrice,
    ProductStatus Status,
    DateTime CreatedAt);
