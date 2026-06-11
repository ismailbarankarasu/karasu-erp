using Karasu.ERP.Shared.Models;
using MediatR;

namespace Karasu.ERP.Application.Features.Products.Queries.GetProductByBarcode;

public record GetProductByBarcodeQuery(string Barcode) : IRequest<Result<ProductBarcodeDto>>;

public record ProductBarcodeDto(
    Guid Id,
    Guid? VariantId,
    string Sku,
    string Name,
    decimal SalePrice,
    decimal TaxRate,
    string? ImageUrl);
