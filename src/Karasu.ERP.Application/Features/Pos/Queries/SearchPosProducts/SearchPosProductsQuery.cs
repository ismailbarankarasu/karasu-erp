using Karasu.ERP.Shared.Models;
using MediatR;

namespace Karasu.ERP.Application.Features.Pos.Queries.SearchPosProducts;

public record SearchPosProductsQuery(string? Barcode, string? Search) : IRequest<Result<IReadOnlyList<PosProductDto>>>;

public record PosProductDto(
    Guid ProductId,
    Guid? VariantId,
    string Sku,
    string? Barcode,
    string Name,
    decimal SalePrice,
    decimal TaxRate,
    decimal? AvailableStock);
