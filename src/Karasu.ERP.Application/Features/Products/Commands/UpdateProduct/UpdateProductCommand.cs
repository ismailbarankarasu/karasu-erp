using Karasu.ERP.Domain.Enums;
using Karasu.ERP.Shared.Models;
using MediatR;

namespace Karasu.ERP.Application.Features.Products.Commands.UpdateProduct;

public record UpdateProductCommand(
    Guid Id,
    string Sku,
    string? Barcode,
    string Name,
    Guid? CategoryId,
    Guid? BrandId,
    Guid UnitId,
    decimal PurchasePrice,
    decimal SalePrice,
    decimal TaxRate,
    decimal MinStock,
    ProductStatus Status) : IRequest<Result>;
