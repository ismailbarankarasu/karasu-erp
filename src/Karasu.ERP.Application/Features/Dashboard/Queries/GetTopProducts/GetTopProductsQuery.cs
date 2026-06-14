using Karasu.ERP.Shared.Models;
using MediatR;

namespace Karasu.ERP.Application.Features.Dashboard.Queries.GetTopProducts;

public record GetTopProductsQuery(int? PeriodDays = 30) : IRequest<Result<List<TopProductDto>>>;

public record TopProductDto(
    string ProductName,
    string Sku,
    decimal QuantitySold,
    decimal Revenue);
