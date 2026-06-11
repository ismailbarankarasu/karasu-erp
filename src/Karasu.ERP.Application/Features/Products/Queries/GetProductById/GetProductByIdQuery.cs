using Karasu.ERP.Domain.Enums;
using Karasu.ERP.Shared.Models;
using MediatR;

namespace Karasu.ERP.Application.Features.Products.Queries.GetProductById;

public record GetProductByIdQuery(Guid Id) : IRequest<Result<ProductDetailDto>>;

public record ProductDetailDto(
    Guid Id,
    string Sku,
    string? Barcode,
    string Name,
    Guid? CategoryId,
    string? CategoryName,
    Guid? BrandId,
    string? BrandName,
    Guid UnitId,
    string UnitName,
    string UnitSymbol,
    decimal PurchasePrice,
    decimal SalePrice,
    decimal TaxRate,
    decimal MinStock,
    string? ImageUrl,
    ProductStatus Status,
    Guid? DefaultVariantId,
    DateTime CreatedAt,
    DateTime? UpdatedAt);
