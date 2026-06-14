using Karasu.ERP.Shared.Models;
using MediatR;

namespace Karasu.ERP.Application.Features.Dashboard.Queries.GetBranchComparison;

public record GetBranchComparisonQuery() : IRequest<Result<List<BranchComparisonDto>>>;

public record BranchComparisonDto(
    Guid BranchId,
    string BranchName,
    decimal TotalSales,
    int OrderCount);
