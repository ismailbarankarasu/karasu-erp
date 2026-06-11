using Karasu.ERP.Shared.Models;
using MediatR;

namespace Karasu.ERP.Application.Features.Products.Commands.CreateProduct;

public record CreateProductCommand(
    string Sku,
    string? Barcode,
    string Name,
    Guid? CategoryId,
    Guid? BrandId,
    Guid UnitId,
    decimal PurchasePrice,
    decimal SalePrice,
    decimal TaxRate,
    decimal MinStock) : IRequest<Result<Guid>>;
